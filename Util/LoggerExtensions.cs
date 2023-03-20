using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace TeamsIntegration.Util;

/// <summary>
/// Extension to log objects as json
/// </summary>
public static class LoggerExtensions
{
    public static void LogObjectAsJson(this ILogger logger, Object message, [CallerMemberName] String memberName = "")
    {
        if (message == null)
        {
            return;
        }

        String json = JsonSerializer.Serialize(message);
        logger.LogInformation("{MemberName}: {Json}", memberName, json);
    }
}
