namespace LootNet_API.DTO.Validation;

using System.Collections;
using System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class MinCollectionCountAttribute : ValidationAttribute
{
    private readonly int _minCount;

    public MinCollectionCountAttribute(int minCount)
    {
        _minCount = minCount;
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return false;

        return value is ICollection collection && collection.Count >= _minCount;
    }
}

