using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;
using LootNet_API.Enums;

namespace LootNet_API.DTO.EnemyGeneration.Create;

public class CreateEnemyClassProfileDTO : IValidatableObject
{
    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public EnemyClass Class { get; set; }

    [Required]
    [MinCollectionCount(1)]
    [MaxCollectionCount(10)]
    public List<int> AllowedColumns { get; set; } = new();

    [NotEmptyGuid]
    public Guid GenerationProfileId { get; set; }

    [Range(0.0001, 1000000)]
    public double Weight { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AllowedColumns.Any(x => x < 0 || x > 4))
            yield return new ValidationResult("Allowed columns must be between 0 and 4.");
    }
}
