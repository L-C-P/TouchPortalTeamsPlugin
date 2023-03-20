using System.Text.Json.Serialization;
using TeamsIntegration.Enums;

namespace TeamsIntegration.Models;

/// <summary>
/// Helper class to send requests to Teams
/// </summary>
public class TeamsWsRequest
{
    public TeamsWsRequest(TeamsAction teamsAction, String manufacturer = null, String apiVersion = null)
    {
        TeamsAction = teamsAction;
        Manufacturer = manufacturer ?? "Acme";
        ApiVersion = apiVersion ?? "1.0.0";
        Device = Environment.MachineName;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public String ApiVersion { get; }

    [JsonIgnore]
    public TeamsAction TeamsAction { get; }
    public String Action => TeamsAction.ToString().ToLower().Replace('_', '-');

    public String Manufacturer { get; }

    public String Device { get; }

    public Int64 Timestamp { get; private set; }

    /// <summary>
    /// Calculate service type for Teams.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public String Service =>
        TeamsAction switch
        {
            TeamsAction.Query_Meeting_State => "query-meeting-state",
            TeamsAction.Toggle_Mute => "toggle-mute",
            TeamsAction.Toggle_Video => "toggle-video",
            TeamsAction.Toggle_Recording => "recording",
            TeamsAction.Toggle_Background_Blur => "background-blur",
            TeamsAction.Toggle_Hand => "raise-hand",
            TeamsAction.Leave_Call => "call",
            TeamsAction.React_Applause => "call",
            TeamsAction.React_Laugh => "call",
            TeamsAction.React_Like => "call",
            TeamsAction.React_Love => "call",
            TeamsAction.React_Surprised => "call",
            _ => throw new ArgumentOutOfRangeException(nameof(TeamsAction), $"Not expected value {TeamsAction}")
        };

    public TeamsWsRequest RequestWithUpdatedTimeStamp()
    {
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return this;
    }
}
