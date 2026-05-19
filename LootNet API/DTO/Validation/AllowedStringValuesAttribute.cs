namespace LootNet_API.DTO.Validation;

using System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class AllowedStringValuesAttribute : ValidationAttribute
{
    private readonly HashSet<string> _values;

    public AllowedStringValuesAttribute(params string[] values)
    {
        _values = values.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        return value is string text && _values.Contains(text);
    }
}

