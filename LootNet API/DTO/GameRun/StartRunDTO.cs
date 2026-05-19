using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;

namespace LootNet_API.DTO.GameRun;

public class StartRunDTO : IValidatableObject
{
    [Required]
    [MaxCollectionCount(20)]
    public required List<Guid> ItemIds { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ItemIds.Any(x => x == Guid.Empty))
            yield return new ValidationResult("Item identifiers cannot be empty.");
    }
}
