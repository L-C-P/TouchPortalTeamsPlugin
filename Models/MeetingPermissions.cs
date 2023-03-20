using TeamsIntegration.Attributes;

namespace TeamsIntegration.Models;

/// <summary>
/// Possible permissions for the current Teams call
/// </summary>
public class MeetingPermissions
{
    [TpStatusBoolean("Yes", "No")]
    public Boolean CanToggleMute { get; set; }
    [TpStatusBoolean("Yes", "No")]
    public Boolean CanToggleVideo { get; set; }
    [TpStatusBoolean("Yes", "No")]
    public Boolean CanToggleHand { get; set; }
    [TpStatusBoolean("Yes", "No")]
    public Boolean CanToggleBlur { get; set; }
    [TpStatusBoolean("Yes", "No")]
    public Boolean CanToggleRecord { get; set; }
    [TpStatusBoolean("Yes", "No")]
    public Boolean CanLeave { get; set; }
    [TpStatusBoolean("Yes", "No")]
    public Boolean CanReact { get; set; }
}
