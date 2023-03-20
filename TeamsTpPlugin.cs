using Microsoft.Extensions.Logging;
using System.Reflection;
using TeamsIntegration.Configurations;
using TeamsIntegration.Enums;
using TeamsIntegration.Util;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;

namespace TeamsIntegration;

/// <summary>
/// This is the TouchPortal plugin
/// </summary>
public class TeamsTpPlugin : PluginBase
{
    private const String CPluginId = "info.sowa.teams";
    private readonly TeamsConfiguration _Configuration;
    private readonly CancellationTokenSource _CancellationTokenSource;

    protected override ILogger Logger { get; }
    protected override ITouchPortalClient Client { get; }
    private readonly TeamsWsIntegration _TeamsWsIntegration;

    public Boolean IsConnected => Client.IsConnected;

    public TeamsTpPlugin(ITouchPortalClientFactory clientFactory, TeamsWsIntegration teamsWsIntegration, ILogger<TeamsTpPlugin> logger)
    {
        _TeamsWsIntegration = teamsWsIntegration;
        Logger = logger;
        Client = clientFactory.Create(this);

        _CancellationTokenSource = new CancellationTokenSource();

        _Configuration = new TeamsConfiguration()
        {
            Protocol = "1.0.0",
            Manufacturer = (GetType().Assembly.GetCustomAttribute(typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute)?.Company,
            App = (GetType().Assembly.GetCustomAttribute(typeof(AssemblyTitleAttribute)) as AssemblyTitleAttribute)?.Title,
            AppVersion = GetType().Assembly.GetName().Version?.ToString()
        };
    }

    protected override String GetPluginId()
    {
        return CPluginId;
    }

    public void Run()
    {
        try
        {
            if (!Client.Connect())
            {
                Logger.LogError("Client could not connect to TouchPortal");
                return;
            }

            _TeamsWsIntegration.Run(this, _Configuration, _CancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Logger.LogError("Run: {ExMessage}", ex.Message);
        }
    }

    public override void OnInfoEvent(InfoEvent message)
    {
        Logger.LogObjectAsJson(message.Settings);
        ConfigurationUpdate(message.Settings);
    }

    public override void OnSettingsEvent(SettingsEvent message)
    {
        Logger.LogObjectAsJson(message.Values);
        ConfigurationUpdate(message.Values);
    }

    public override void OnClosedEvent(String message)
    {
        try
        {
            _CancellationTokenSource.Cancel();
        }
        catch (Exception ex)
        {
            Logger.LogError("OnClosedEvent: {ExMessage}", ex.Message);
        }
        finally
        {
            Environment.Exit(0);
        }
    }

    public override void OnActionEvent(ActionEvent message)
    {
        try
        {
            Logger.LogTrace("Event {MessageType}", message.Type);

            switch (message.Type)
            {
                case "action":
                    Logger.LogTrace("Action {ActionId}", message.ActionId);

                    switch (message.ActionId)
                    {
                        case CPluginId + ".action.toggle.microphone":
                            _TeamsWsIntegration.Command(TeamsAction.Toggle_Mute, GetTpActionState(message));
                            break;
                        case CPluginId + ".action.toggle.camera":
                            _TeamsWsIntegration.Command(TeamsAction.Toggle_Video, GetTpActionState(message));
                            break;
                        case CPluginId + ".action.toggle.recording":
                            _TeamsWsIntegration.Command(TeamsAction.Toggle_Recording, GetTpActionState(message));
                            break;
                        case CPluginId + ".action.toggle.backgroundblur":
                            _TeamsWsIntegration.Command(TeamsAction.Toggle_Background_Blur, GetTpActionState(message));
                            break;
                        case CPluginId + ".action.toggle.hand":
                            _TeamsWsIntegration.Command(TeamsAction.Toggle_Hand, GetTpActionState(message));
                            break;
                        case CPluginId + ".action.call.leave":
                            _TeamsWsIntegration.Command(TeamsAction.Leave_Call);
                            break;
                        case CPluginId + ".action.react.applause":
                            _TeamsWsIntegration.Command(TeamsAction.React_Applause);
                            break;
                        case CPluginId + ".action.react.laugh":
                            _TeamsWsIntegration.Command(TeamsAction.React_Laugh);
                            break;
                        case CPluginId + ".action.react.like":
                            _TeamsWsIntegration.Command(TeamsAction.React_Like);
                            break;
                        case CPluginId + ".action.react.love":
                            _TeamsWsIntegration.Command(TeamsAction.React_Love);
                            break;
                        case CPluginId + ".action.react.surprised":
                            _TeamsWsIntegration.Command(TeamsAction.React_Surprised);
                            break;
                    }

                    break;
                case "up":
                    break;
                case "down":
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("OnActionEvent: {ExMessage}", ex.Message);
        }
    }

    public Boolean SendStateUpdate(String state, Object update)
    {
        return Client.IsConnected && Client.StateUpdate($"{CPluginId}.state.{state.ToLowerInvariant()}", update.ToString());
    }

    private void ConfigurationUpdate(IEnumerable<Setting> settings)
    {
        foreach (Setting setting in settings)
        {
            switch (setting.Name)
            {
                case "API token":
                    _Configuration.ApiToken = setting.Value.Trim();
                    break;
                case "Teams address":
                    _Configuration.TeamsIpDns = setting.Value.Trim();
                    break;
                // case "Restore mic/video (Yes/No)":
                //     _Configuration.RestoreCamVideo = setting.Value.ToLowerInvariant().Trim() == "yes";
                //     break;
            }
        }

        _Configuration.ConfigurationChanged = true;
    }

    private TpActionState GetTpActionState(ActionEvent message)
    {
        String set = message.Data.FirstOrDefault(d => d.Id == $"{message.ActionId}.set")?.Value;

        if (!Enum.TryParse(set ?? "Toggle", true, out TpActionState tpAction))
        {
            Logger.LogError("Error parsing state {Set}", set);
        }

        return tpAction;
    }
}
