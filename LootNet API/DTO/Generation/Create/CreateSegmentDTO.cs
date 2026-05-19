using System.ComponentModel.DataAnnotations;

namespace LootNet_API.DTO.Generation.Create;

public class CreateSegmentDTO : IValidatableObject
{
    [Range(-1000000, 1000000)]
    public double Min { get; set; }

    [Range(-1000000, 1000000)]
    public double Max { get; set; }

    [Range(1, 1000000)]
    public int Weight { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Min > Max)
            yield return new ValidationResult("Segment minimum cannot be greater than maximum.");
    }
}

