using Microsoft.Extensions.Logging;
using TouchPortalSDK;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;

namespace TeamsIntegration.Util;

/// <summary>
/// Base class to handle some TouchPortal logging
/// </summary>
public abstract class PluginBase : ITouchPortalEventHandler
{
    public String PluginId => GetPluginId();

    protected abstract ILogger Logger { get; }

    protected abstract ITouchPortalClient Client { get; }

    protected abstract String GetPluginId();

    public virtual void OnActionEvent(ActionEvent message)
    {
        Logger.LogObjectAsJson(message);
    }

    public virtual void OnBroadcastEvent(BroadcastEvent message)
    {
        Logger.LogObjectAsJson(message);
    }

    public virtual void OnClosedEvent(String message)
    {
        Logger.LogObjectAsJson(message);

        //Optional force exits this plugin.
        Environment.Exit(0);
    }

    public virtual void OnConnecterChangeEvent(ConnectorChangeEvent message)
    {
        Logger.LogObjectAsJson(message);
    }

    public virtual void OnInfoEvent(InfoEvent message)
    {
        Logger.LogObjectAsJson(message);
    }

    public virtual void OnListChangedEvent(ListChangeEvent message)
    {
        Logger.LogObjectAsJson(message);
    }

    public virtual void OnNotificationOptionClickedEvent(NotificationOptionClickedEvent message)
    {
        Logger.LogObjectAsJson(message);
    }

    public virtual void OnSettingsEvent(SettingsEvent message)
    {
        Logger.LogObjectAsJson(message);
    }

    public virtual void OnShortConnectorIdNotificationEvent(ConnectorInfo connectorInfo)
    {
        Logger.LogObjectAsJson(connectorInfo);
    }

    public virtual void OnUnhandledEvent(String jsonMessage)
    {
        Logger.LogObjectAsJson(jsonMessage);
    }
}
