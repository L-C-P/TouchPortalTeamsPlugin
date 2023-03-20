// ReSharper disable InconsistentNaming
namespace TeamsIntegration.Enums;

/// <summary>
/// Possible actions to trigger in Teams
/// </summary>
public enum TeamsAction
{
    Query_Meeting_State,
    Toggle_Mute,
    Toggle_Video,
    Toggle_Recording, // Not implemented
    Leave_Call,
    Toggle_Background_Blur,
    Toggle_Hand,
    React_Applause,
    React_Laugh,
    React_Like,
    React_Love,
    React_Surprised // Not implemented
}
