namespace TeamsIntegration.Attributes;

/// <summary>
/// Attribute to set a different name for a state
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class TpStatusNameAttribute : Attribute
{
    public String EventName { get; }

    public TpStatusNameAttribute(String eventName)
    {
        EventName = eventName;
    }
}
