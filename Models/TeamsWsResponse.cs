namespace TeamsIntegration.Models;

/// <summary>
/// Model to handle response from Teams
/// </summary>
public class TeamsWsResponse
{
    public String ApiVersion { get; set; }
    public MeetingUpdate MeetingUpdate { get; set; } = new MeetingUpdate();
}
