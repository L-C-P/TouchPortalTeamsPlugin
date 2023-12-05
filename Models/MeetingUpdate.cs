namespace TeamsIntegration.Models;

/// <summary>
///     State / permission update from Teams
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class MeetingUpdate
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public MeetingState MeetingState { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public MeetingPermissions MeetingPermissions { get; set; }
}
