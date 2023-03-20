using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TeamsIntegration.Attributes;
using TeamsIntegration.Configurations;
using TeamsIntegration.Enums;
using TeamsIntegration.Models;

namespace TeamsIntegration;

/// <summary>
/// This will handle the Teams communication
/// </summary>
public class TeamsWsIntegration
{
    private const String CTeamsWsUriPattern = "ws://{0}:8124?token={1}&protocol-version={2}&manufacturer={3}&device={4}&app={5}&app-version={6}";
    private readonly ILogger<TeamsWsIntegration> _Logger;
    private readonly JsonSerializerOptions _SerializeOptions;
    private TeamsTpPlugin _TeamsTpPlugin;
    private CancellationToken _CancellationToken;
    private TeamsWsRequest _TeamsStatusRequest;
    private ClientWebSocket _WebSocket;
    private TeamsConfiguration _Configuration;
    private TeamsWsResponse _LastTeamsWsResponse;

    public TeamsWsIntegration(ILogger<TeamsWsIntegration> logger)
    {
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _SerializeOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
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

        _TeamsStatusRequest = new TeamsWsRequest(TeamsAction.Query_Meeting_State, configuration.Manufacturer, configuration.Protocol);
        Byte[] buffer = new Byte[512];
        System.Timers.Timer timer = new System.Timers.Timer(10000);

        timer.Elapsed += (o, args) =>
        {
            try
            {
                Send(_TeamsStatusRequest.RequestWithUpdatedTimeStamp());
            }
            catch { }
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                ConnectToTeams(cancellationToken);
                timer.Start();

                while (_WebSocket.State == WebSocketState.Open && !configuration.ConfigurationChanged)
                {
                    WebSocketReceiveResult result = _WebSocket.ReceiveAsync(buffer, cancellationToken).Result;

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        timer.Stop();
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
                        _LastTeamsWsResponse = CheckForChangesInResponse(_LastTeamsWsResponse, newTeamsWsResponse);
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
                _Logger.LogError("{Ex}", ex.ToString());
#else
                _Logger.LogError("{Ex}", ex.Message);
#endif
            }
            finally
            {
                timer.Stop();
                CleanupWebSocket();

                if (!cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                }
            }
        }
    }

    private void ConnectToTeams(CancellationToken cancellationToken)
    {
        _Configuration.ConfigurationChanged = false;

        Uri uri = new Uri(String.Format(CTeamsWsUriPattern, _Configuration.TeamsIpDns, _Configuration.ApiToken, _Configuration.Protocol, _Configuration.Manufacturer, Environment.MachineName, _Configuration.App, _Configuration.AppVersion));
        _Logger.LogInformation("Connect to Teams with {Uri}", uri.AbsoluteUri);
        _WebSocket = new ClientWebSocket();
        _WebSocket.ConnectAsync(uri, cancellationToken).Wait(cancellationToken);
        _TeamsTpPlugin.SendStateUpdate("isconnected", "Yes");

        // Request state from Teams
        _LastTeamsWsResponse = null;
        Send(_TeamsStatusRequest.RequestWithUpdatedTimeStamp());
    }

    private void CleanupWebSocket()
    {
        _TeamsTpPlugin.SendStateUpdate("isconnected", "No");

        _WebSocket?.Abort();
        _WebSocket?.Dispose();
        _WebSocket = null;
    }


    private TeamsWsResponse CheckForChangesInResponse(Object response1, Object response2)
    {
        if (!_TeamsTpPlugin.IsConnected)
        {
            return null;
        }

        Type currentType = response2.GetType();
        PropertyInfo[] propertyInfos = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo propertyInfo in propertyInfos)
        {
            Object value1 = response1 != null
                ? currentType.GetProperty(propertyInfo.Name)?.GetValue(response1)
                : null;
            Object value2 = currentType.GetProperty(propertyInfo.Name)?.GetValue(response2);

            if (propertyInfo.PropertyType.IsPrimitive || propertyInfo.PropertyType == typeof(String))
            {
                if (value1 != value2 && (value1 == null || !value1.Equals(value2)))
                {
                    _Logger.LogInformation("Value {Name} changed from {Value1} to {Value2}", propertyInfo.Name, value1, value2);

                    String eventName = propertyInfo.Name;
                    eventName = HandleStatusNameAttribute(propertyInfo, eventName);
                    value2 = HandleStatusBooleanAttribute(propertyInfo, value2);
                    _TeamsTpPlugin.SendStateUpdate(eventName, value2);
                }
            }
            else if (propertyInfo.PropertyType.IsClass)
            {
                CheckForChangesInResponse(value1, value2);
            }
        }

        return response2 as TeamsWsResponse;
    }

    private static Object HandleStatusBooleanAttribute(PropertyInfo propertyInfo, Object value)
    {
        TpStatusBooleanAttribute statusBooleanAttribute = propertyInfo.GetCustomAttribute<TpStatusBooleanAttribute>();

        if (statusBooleanAttribute != null)
        {
            Boolean boolValue = Convert.ToBoolean(value);

            if (!String.IsNullOrEmpty(statusBooleanAttribute.TrueText) && boolValue)
            {
                value = statusBooleanAttribute.TrueText;
            }

            if (!String.IsNullOrEmpty(statusBooleanAttribute.FalseText) && !boolValue)
            {
                value = statusBooleanAttribute.FalseText;
            }
        }

        return value;
    }

    private static String HandleStatusNameAttribute(PropertyInfo propertyInfo, String eventName)
    {
        TpStatusNameAttribute statusNameAttribute = propertyInfo.GetCustomAttribute<TpStatusNameAttribute>();

        return statusNameAttribute != null && !String.IsNullOrEmpty(statusNameAttribute.EventName)
            ? statusNameAttribute.EventName
            : eventName;
    }

    public void Command(TeamsAction action, TpActionState? actionState = null)
    {
        TeamsWsRequest teamsWsRequest = new TeamsWsRequest(action, _Configuration.Manufacturer, _Configuration.Protocol);

        switch (action)
        {
            case TeamsAction.Toggle_Mute:
                if (actionState == TpActionState.Toggle ||
                    (actionState == TpActionState.On && _LastTeamsWsResponse.MeetingUpdate.MeetingState.IsMuted) ||
                    (actionState == TpActionState.Off && !_LastTeamsWsResponse.MeetingUpdate.MeetingState.IsMuted))
                {
                    Send(teamsWsRequest);
                }

                break;
            case TeamsAction.Toggle_Video:
                if (actionState == TpActionState.Toggle ||
                    (actionState == TpActionState.On && !_LastTeamsWsResponse.MeetingUpdate.MeetingState.IsCameraOn) ||
                    (actionState == TpActionState.Off && _LastTeamsWsResponse.MeetingUpdate.MeetingState.IsCameraOn))
                {
                    Send(teamsWsRequest);
                }

                break;
            case TeamsAction.Toggle_Recording:
                if (actionState == TpActionState.Toggle ||
                    (actionState == TpActionState.On && !_LastTeamsWsResponse.MeetingUpdate.MeetingState.IsRecordingOn) ||
                    (actionState == TpActionState.Off && _LastTeamsWsResponse.MeetingUpdate.MeetingState.IsRecordingOn))
                {
                    Send(teamsWsRequest);
                }

                break;
            case TeamsAction.Toggle_Background_Blur:
                if (actionState == TpActionState.Toggle ||
                    (actionState == TpActionState.On && !_LastTeamsWsResponse.MeetingUpdate.MeetingState.IsBackgroundBlurred) ||
                    (actionState == TpActionState.Off && _LastTeamsWsResponse.MeetingUpdate.MeetingState.IsBackgroundBlurred))
                {
                    Send(teamsWsRequest);
                }

                break;
            case TeamsAction.Toggle_Hand:
                if (actionState == TpActionState.Toggle ||
                    (actionState == TpActionState.Up && !_LastTeamsWsResponse.MeetingUpdate.MeetingState.IsHandRaised) ||
                    (actionState == TpActionState.Down && _LastTeamsWsResponse.MeetingUpdate.MeetingState.IsHandRaised))
                {
                    Send(teamsWsRequest);
                }

                break;
            default:
                Send(teamsWsRequest);
                break;
        }
    }

    private void Send(TeamsWsRequest request)
    {
        if (_CancellationToken.IsCancellationRequested || _WebSocket?.State != WebSocketState.Open)
        {
            return;
        }

        String requestMessage = JsonSerializer.Serialize(request, _SerializeOptions);
        _Logger.LogTrace("Send request to Teams: {RequestMessage}", requestMessage);
        _WebSocket.SendAsync(Encoding.UTF8.GetBytes(requestMessage), WebSocketMessageType.Text, true, _CancellationToken).Wait(_CancellationToken);
    }
}
