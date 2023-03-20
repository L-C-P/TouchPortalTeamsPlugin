using TeamsIntegration.Attributes;

namespace TeamsIntegration.Models;

/// <summary>
/// Meeting state for the current call
/// </summary>
public class MeetingState
{
    [TpStatusName("IsMicrophoneOn")]
    [TpStatusBoolean("Off", "On")]
    public Boolean IsMuted { get; set; }

    [TpStatusBoolean("On", "Off")]
    public Boolean IsCameraOn { get; set; }

    [TpStatusBoolean("Up", "Down")]
    public Boolean IsHandRaised { get; set; }

    [TpStatusBoolean("Yes", "No")]
    public Boolean IsInMeeting { get; set; }

    [TpStatusBoolean("On", "Off")]
    public Boolean IsRecordingOn { get; set; }

    [TpStatusBoolean("On", "Off")]
    public Boolean IsBackgroundBlurred { get; set; }
}
