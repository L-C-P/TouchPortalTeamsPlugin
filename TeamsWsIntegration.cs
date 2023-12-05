using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeamsIntegration.Configurations;
using TeamsIntegration.Enums;
using TeamsIntegration.Models;
using TeamsIntegration.Util;

namespace TeamsIntegration;

/// <summary>
///     This will handle the Teams communication
/// </summary>
public class TeamsWsIntegration
{
    private const String CTeamsWsUriPattern = "ws://{0}:8124?token={1}&protocol-version=2.0.0&manufacturer={2}&device={3}&app={4}&app-version={5}";
    private readonly ILogger<TeamsWsIntegration> _Logger;
    private readonly JsonSerializerOptions _SerializeOptions;
    private CancellationToken _CancellationToken;
    private TeamsConfiguration _Configuration;
    private TeamsWsResponse _LastTeamsWsResponse;
    private Int32 _PairRequestId = -1;
    private TeamsTpPlugin _TeamsTpPlugin;
    private ClientWebSocket _WebSocket;

    public TeamsWsIntegration(ILogger<TeamsWsIntegration> logger)
    {
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _SerializeOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    private void ConnectToTeams(CancellationToken cancellationToken)
    {
        _Configuration.ConfigurationChanged = false;

        Uri uri = new Uri(String.Format(CTeamsWsUriPattern, _Configuration.TeamsIpDns, _Configuration.ApiToken, _Configuration.Manufacturer, Environment.MachineName, _Configuration.App, _Configuration.AppVersion));
        _Logger.LogInformation("Connect to Teams with {Uri}", uri.AbsoluteUri);
        _WebSocket = new ClientWebSocket();
        _WebSocket.ConnectAsync(uri, cancellationToken).Wait(cancellationToken);
        // _TeamsTpPlugin.SendStateUpdate("lastteamsstatus", "Connected");
        _TeamsTpPlugin.SendStateUpdate("ispaired", _Configuration.Paired
                                           ? "Yes"
                                           : "No");
        _LastTeamsWsResponse = null;
    }

    public void Run(TeamsTpPlugin teamsTpPlugin, TeamsConfiguration configuration, CancellationToken cancellationToken)
    {
        _TeamsTpPlugin = teamsTpPlugin;
        _Configuration = configuration;
        _CancellationToken = cancellationToken;

        // Wait for initial configuration from TP
        while (!configuration.ConfigurationChanged)
        {
            cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        Byte[] buffer = new Byte[512];

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                ConnectToTeams(cancellationToken);

                while (_WebSocket.State == WebSocketState.Open && !configuration.ConfigurationChanged)
                {
                    WebSocketReceiveResult result = _WebSocket.ReceiveAsync(buffer, cancellationToken).Result;

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken).Wait(cancellationToken);
                    }
                    else
                    {
                        if (result.Count > buffer.Length)
                        {
                            throw new Exception($"WS result buffer is to small. {result.Count} bytes needed.");
                        }

                        String responseMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _Logger.LogTrace("Got message from Teams: {ResponseMessage}", responseMessage);

                        TeamsWsResponse newTeamsWsResponse = JsonSerializer.Deserialize<TeamsWsResponse>(responseMessage, _SerializeOptions);

                        if (newTeamsWsResponse?.MeetingUpdate != null)
                        {
                            if (!_Configuration.Paired && newTeamsWsResponse.MeetingUpdate.MeetingPermissions.CanPair && _PairRequestId < 0)
                            {
                                _Logger.LogTrace("Pair with Teams");
                                // _TeamsTpPlugin.SendStateUpdate("lastteamsstatus", "Pair with Teams");
                                TeamsWsRequest teamsWsRequest = Command(TeamsAction.React_Like);
                                _PairRequestId = teamsWsRequest.RequestId;
                            }
                            else
                            {
                                _LastTeamsWsResponse = PropertyComparer.CheckForChangesInResponse(_LastTeamsWsResponse, newTeamsWsResponse, _TeamsTpPlugin, _Logger);
                            }
                        }

                        if (newTeamsWsResponse?.TokenRefresh != null)
                        {
                            _Logger.LogInformation("Got API token from Teams");

                            _Configuration.ApiToken = newTeamsWsResponse.TokenRefresh;
                            _Configuration.Paired = true;
                            _Configuration.Save();
                            // _Configuration.ConfigurationChanged = true;

                            _TeamsTpPlugin.SendStateUpdate("ispaired", "Yes");
                        }

                        if (newTeamsWsResponse?.Response != null)
                        {
                            if (newTeamsWsResponse.RequestId == _PairRequestId)
                            {
                                _PairRequestId = -1;
                                _Logger.LogInformation("Pair with Teams successful");
                                // _TeamsTpPlugin.SendStateUpdate("lastteamsstatus", "Pair with Teams successful");
                            }
                            else
                            {
                                _Logger.LogTrace("Response for id {id} from Teams: {response}", newTeamsWsResponse.RequestId, newTeamsWsResponse.Response);
                                // _TeamsTpPlugin.SendStateUpdate("lastteamsstatus", newTeamsWsResponse.Response);
                            }
                        }

                        if (newTeamsWsResponse?.ErrorMsg != null)
                        {
                            if (newTeamsWsResponse.RequestId == _PairRequestId)
                            {
                                _PairRequestId = -1;
                                _Logger.LogError("Pair with Teams failed: {errorMessage}", newTeamsWsResponse.ErrorMsg);
                                // _TeamsTpPlugin.SendStateUpdate("lastteamsstatus", $"Pair with Teams failed: {newTeamsWsResponse.ErrorMsg}");
                            }
                            else
                            {
                                _Logger.LogError("Error for id {id} from Teams: {errorMsg}", newTeamsWsResponse.RequestId, newTeamsWsResponse.ErrorMsg);
                                // _TeamsTpPlugin.SendStateUpdate("lastteamsstatus", newTeamsWsResponse.ErrorMsg);
                            }
                        }
                    }
                }

                if (configuration.ConfigurationChanged)
                {
                    _Logger.LogInformation("Restart connection, because of configuration change");
                }
                else
                {
                    _Logger.LogInformation("Teams connection closed {WebSocketState}", _WebSocket.State);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                String errorMessage = ex.ToString();
#else
                String errorMessage = ex.Message;
#endif
                if (errorMessage.Contains("403"))
                {
                    _Configuration.Paired = false;
                    _Configuration.Save();
                    _PairRequestId = -1;
                    _Logger.LogError("Plugin is blocked in Teams");
                    // _TeamsTpPlugin.SendStateUpdate("lastteamsstatus", "Plugin is blocked in Teams");
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(60));
                }
                else
                {
                    _Logger.LogError("{Ex}", errorMessage);
                    // _TeamsTpPlugin.SendStateUpdate("lastteamsstatus", errorMessage);
                }
            }
            finally
            {
                CleanupWebSocket();

                if (!cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                }
            }
        }
    }

    private void CleanupWebSocket()
    {
        // ReSharper disable once StringLiteralTypo
        _TeamsTpPlugin.SendStateUpdate("isconnected", "No");

        _WebSocket?.Abort();
        _WebSocket?.Dispose();
        _WebSocket = null;
    }

    public TeamsWsRequest Command(TeamsAction action, TpActionState? actionState = null)
    {
        TeamsWsRequest teamsWsRequest = new TeamsWsRequest(action);

        switch (action)
        {
            case TeamsAction.Toggle_Mute:
                if (actionState == TpActionState.Toggle ||
                    (actionState == TpActionState.On && _LastTeamsWsResponse?.MeetingUpdate?.MeetingState.IsMuted == true) ||
                    (actionState == TpActionState.Off && _LastTeamsWsResponse?.MeetingUpdate?.MeetingState.IsMuted == false))
                {
                    Send(teamsWsRequest);
                }

                break;
            case TeamsAction.Toggle_Video:
                if (actionState == TpActionState.Toggle ||
                    (actionState == TpActionState.On && _LastTeamsWsResponse?.MeetingUpdate?.MeetingState.IsVideoOn == false) ||
                    (actionState == TpActionState.Off && _LastTeamsWsResponse?.MeetingUpdate?.MeetingState.IsVideoOn == true))
                {
                    Send(teamsWsRequest);
                }

                break;
            case TeamsAction.Toggle_Background_Blur:
                if (actionState == TpActionState.Toggle ||
                    (actionState == TpActionState.On && _LastTeamsWsResponse?.MeetingUpdate?.MeetingState.IsBackgroundBlurred == false) ||
                    (actionState == TpActionState.Off && _LastTeamsWsResponse?.MeetingUpdate?.MeetingState.IsBackgroundBlurred == true))
                {
                    Send(teamsWsRequest);
                }

                break;
            case TeamsAction.Toggle_Hand:
                if (actionState == TpActionState.Toggle ||
                    (actionState == TpActionState.Up && _LastTeamsWsResponse?.MeetingUpdate?.MeetingState.IsHandRaised == false) ||
                    (actionState == TpActionState.Down && _LastTeamsWsResponse?.MeetingUpdate?.MeetingState.IsHandRaised == true))
                {
                    Send(teamsWsRequest);
                }

                break;
            default:
                Send(teamsWsRequest);

                break;
        }

        return teamsWsRequest;
    }

    private void Send(TeamsWsRequest request)
    {
        if (_CancellationToken.IsCancellationRequested || _WebSocket?.State != WebSocketState.Open)
        {
            _Logger.LogTrace("WebSocked closed");
            return;
        }

        String requestMessage = JsonSerializer.Serialize(request, _SerializeOptions);
        _Logger.LogTrace("Send request to Teams: {RequestMessage}", requestMessage);
        _WebSocket.SendAsync(Encoding.UTF8.GetBytes(requestMessage), WebSocketMessageType.Text, true, _CancellationToken).Wait(_CancellationToken);
    }
}
