using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;

namespace LootNet_API.DTO.Generation.Update;

public class UpdateGenerationProfileDTO
{
    [NotEmptyGuid]
    public Guid Id { get; set; }

    [StringLength(80, MinimumLength = 1)]
    public string? Name { get; set; }
}

