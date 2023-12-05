using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeamsIntegration.Configurations;

/// <summary>
///     Current Teams state/configuration
/// </summary>
public class TeamsConfiguration
{
    private readonly ILogger? _Logger;
    private readonly String _SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TPTeams");
    private readonly String _SettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TPTeams", "settings.json");

    public TeamsConfiguration()
    {
    }

    public TeamsConfiguration(ILogger logger)
    {
        _Logger = logger;
    }

    [JsonIgnore]
    public String TeamsIpDns { get; set; } = "localhost";

    public String ApiToken { get; set; } = Guid.NewGuid().ToString();

    [JsonIgnore]
    public String Manufacturer { get; init; }

    [JsonIgnore]
    public String App { get; init; }

    [JsonIgnore]
    public String AppVersion { get; init; }

    public Boolean Paired { get; set; }

    [JsonIgnore]
    public Boolean ConfigurationChanged { get; set; }

    public void Load()
    {
        if (!File.Exists(_SettingsFile))
        {
            return;
        }

        _Logger?.LogTrace("Load settings from {_SettingsFile}", _SettingsFile);
        String json = File.ReadAllText(_SettingsFile);
        TeamsConfiguration? teamsConfiguration = JsonSerializer.Deserialize<TeamsConfiguration>(json);

        if (teamsConfiguration != null)
        {
            ApiToken = teamsConfiguration.ApiToken;
            Paired = teamsConfiguration.Paired;
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(_SettingsPath);
        _Logger?.LogTrace("Save settings to {_SettingsFile}", _SettingsFile);
        String json = JsonSerializer.Serialize(this);
        File.WriteAllText(_SettingsFile, json);
    }
}
