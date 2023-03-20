namespace TeamsIntegration.Configurations;

/// <summary>
/// Current Teams state/configuration
/// </summary>
public class TeamsConfiguration
{
    public String TeamsIpDns { get; set; }
    public String ApiToken { get; set; }
    public Boolean RestoreCamVideo { get; set; }
    public String Protocol { get; set; }
    public String Manufacturer { get; set; }
    public String App { get; set; }
    public String AppVersion { get; set; }
    public Boolean ConfigurationChanged { get; set; }
}
