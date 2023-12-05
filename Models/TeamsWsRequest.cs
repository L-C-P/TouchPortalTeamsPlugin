using System.Text.Json.Serialization;
using TeamsIntegration.Enums;

namespace TeamsIntegration.Models;

/// <summary>
///     Helper class to send requests to Teams
/// </summary>
public class TeamsWsRequest
{
    private static Int32 _RequestId;

    public TeamsWsRequest(TeamsAction teamsAction)
    {
        TeamsAction = teamsAction;
        Parameters = new TeamsWsRequestType(teamsAction);
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Int32 RequestId { get; private set; } = Interlocked.Increment(ref _RequestId);

    [JsonIgnore]
    public TeamsAction TeamsAction { get; }

    public String Action =>
        TeamsAction switch
        {
            TeamsAction.Toggle_Mute => "toggle-mute",
            TeamsAction.Toggle_Video => "toggle-video",
            TeamsAction.Toggle_Background_Blur => "toggle-background-blur",
            TeamsAction.Toggle_Hand => "toggle-hand",
            TeamsAction.Leave_Call => "leave-call",
            TeamsAction.React_Applause => "send-reaction",
            TeamsAction.React_Laugh => "send-reaction",
            TeamsAction.React_Like => "send-reaction",
            TeamsAction.React_Love => "send-reaction",
            TeamsAction.React_Surprised => "send-reaction",
            _ => throw new ArgumentOutOfRangeException(nameof(TeamsAction), $"Not expected value {TeamsAction}")
        };

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public TeamsWsRequestType Parameters { get; }
}
