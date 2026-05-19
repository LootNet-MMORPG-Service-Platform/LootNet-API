using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;

namespace LootNet_API.DTO.Generation.Create;

public class CreateGenerationProfileFullDTO
{
    [Required]
    [StringLength(80, MinimumLength = 1)]
    public required string Name { get; set; }

    [MaxCollectionCount(50)]
    public List<CreateTypeWeightDTO> TypeWeights { get; set; } = new();

    [MaxCollectionCount(100)]
    public List<CreateRuleFullDTO> Rules { get; set; } = new();
}
