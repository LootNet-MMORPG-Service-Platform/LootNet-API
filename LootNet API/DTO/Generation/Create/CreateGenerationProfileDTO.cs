using System.ComponentModel.DataAnnotations;

namespace LootNet_API.DTO.Generation.Create;

public class CreateGenerationProfileDTO
{
    [Required]
    [StringLength(80, MinimumLength = 1)]
    public required string Name { get; set; }
}

