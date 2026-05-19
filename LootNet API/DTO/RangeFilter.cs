namespace LootNet_API.DTO;

using System.ComponentModel.DataAnnotations;

public class RangeFilter<T> : IValidatableObject where T : struct, IComparable<T>
{
    public T? Min { get; set; }
    public T? Max { get; set; }

    public bool IsValid =>
        !(Min.HasValue && Max.HasValue && Min.Value.CompareTo(Max.Value) > 0);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsValid)
            yield return new ValidationResult("Range minimum cannot be greater than range maximum.");
    }
}

