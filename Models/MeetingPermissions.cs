using TeamsIntegration.Attributes;

namespace TeamsIntegration.Models;

/// <summary>
///     Possible permissions for the current Teams call
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
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
    public Boolean CanLeave { get; set; }

    [TpStatusBoolean("Yes", "No")]
    public Boolean CanReact { get; set; }

    [TpStatusBoolean("Yes", "No")]
    public Boolean CanToggleShareTray { get; set; }

    [TpStatusBoolean("Yes", "No")]
    public Boolean CanToggleChat { get; set; }

    [TpStatusBoolean("Yes", "No")]
    public Boolean CanStopSharing { get; set; }

    [TpStatusBoolean("Yes", "No")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Boolean CanPair { get; set; }
}
