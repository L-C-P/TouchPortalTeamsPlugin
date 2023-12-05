using Microsoft.Extensions.Logging;
using System.Reflection;
using TeamsIntegration.Attributes;
using TeamsIntegration.Models;

namespace TeamsIntegration.Util;

public static class PropertyComparer
{
    public static TeamsWsResponse CheckForChangesInResponse(Object response1, Object response2, TeamsTpPlugin teamsTpPlugin, ILogger logger)
    {
        if (response2 == null)
        {
            return null;
        }

        Type currentType = response2.GetType();
        PropertyInfo[] propertyInfos = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo propertyInfo in propertyInfos)
        {
            Object value1 = response1 != null
                ? currentType.GetProperty(propertyInfo.Name)?.GetValue(response1)
                : null;
            Object value2 = currentType.GetProperty(propertyInfo.Name)?.GetValue(response2);

            if (propertyInfo.PropertyType.IsPrimitive || propertyInfo.PropertyType == typeof(String))
            {
                if (value1 != value2 && (value1 == null || !value1.Equals(value2)))
                {
                    logger.LogInformation("Value {Name} changed from {Value1} to {Value2}", propertyInfo.Name, value1, value2);

                    String eventName = propertyInfo.Name;
                    eventName = HandleStatusNameAttribute(propertyInfo, eventName);
                    value2 = HandleStatusBooleanAttribute(propertyInfo, value2);
                    teamsTpPlugin.SendStateUpdate(eventName, value2);
                }
            }
            else if (propertyInfo.PropertyType.IsClass)
            {
                CheckForChangesInResponse(value1, value2, teamsTpPlugin, logger);
            }
        }

        return response2 as TeamsWsResponse;
    }

    private static Object HandleStatusBooleanAttribute(MemberInfo propertyInfo, Object value)
    {
        TpStatusBooleanAttribute statusBooleanAttribute = propertyInfo.GetCustomAttribute<TpStatusBooleanAttribute>();

        if (statusBooleanAttribute != null)
        {
            Boolean boolValue = Convert.ToBoolean(value);

            if (!String.IsNullOrEmpty(statusBooleanAttribute.TrueText) && boolValue)
            {
                value = statusBooleanAttribute.TrueText;
            }

            if (!String.IsNullOrEmpty(statusBooleanAttribute.FalseText) && !boolValue)
            {
                value = statusBooleanAttribute.FalseText;
            }
        }

        return value;
    }

    private static String HandleStatusNameAttribute(MemberInfo propertyInfo, String eventName)
    {
        TpStatusNameAttribute statusNameAttribute = propertyInfo.GetCustomAttribute<TpStatusNameAttribute>();

        return statusNameAttribute != null && !String.IsNullOrEmpty(statusNameAttribute.EventName)
            ? statusNameAttribute.EventName
            : eventName;
    }
}
