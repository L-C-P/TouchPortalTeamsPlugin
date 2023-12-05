namespace TeamsIntegration.Attributes;

/// <summary>
/// Attribute to convert boolean values. E.g to On and Off.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class TpStatusBooleanAttribute : Attribute
{
    public String TrueText { get; }
    public String FalseText { get; }
    public TpStatusBooleanAttribute(String trueText, String falseText)
    {
        TrueText = trueText;
        FalseText = falseText;
    }
}
