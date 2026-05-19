namespace LootNet_API.DTO.Validation;

using System.Collections;
using System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class MaxCollectionCountAttribute : ValidationAttribute
{
    private readonly int _maxCount;

    public MaxCollectionCountAttribute(int maxCount)
    {
        _maxCount = maxCount;
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        return value is ICollection collection && collection.Count <= _maxCount;
    }
}

