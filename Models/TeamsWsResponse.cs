#nullable enable
namespace TeamsIntegration.Models;

/// <summary>
///     Model to handle response from Teams
/// </summary>
public class TeamsWsResponse
{
    public MeetingUpdate? MeetingUpdate { get; set; }

    public Int32? RequestId { get; set; }

    public String? Response { get; set; }

    public String? ErrorMsg { get; set; }

    public String? TokenRefresh { get; set; }
}
