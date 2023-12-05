using System.Text.Json.Serialization;
using TeamsIntegration.Enums;

namespace TeamsIntegration.Models;

public class TeamsWsRequestType
{
    public TeamsWsRequestType(TeamsAction teamsAction)
    {
        TeamsAction = teamsAction;
    }

    [JsonIgnore]
    public TeamsAction TeamsAction { get; }

    public String Type =>
        TeamsAction switch
        {
            TeamsAction.React_Applause => "applause",
            TeamsAction.React_Laugh => "laugh",
            TeamsAction.React_Like => "like",
            TeamsAction.React_Love => "love",
            TeamsAction.React_Surprised => "wow",
            _ => null
        };
}
