namespace TeamsIntegration.Models;

/// <summary>
/// State / permission update from Teams
/// </summary>
public class MeetingUpdate
{
    public MeetingState MeetingState { get; set; } = new MeetingState();
    public MeetingPermissions MeetingPermissions { get; set; } = new MeetingPermissions();
}
