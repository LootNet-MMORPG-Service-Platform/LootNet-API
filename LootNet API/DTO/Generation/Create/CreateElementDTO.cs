using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;
using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Create;

public class CreateElementDTO
{
    public ItemElementType ElementType { get; set; }

    [Required]
    [MinCollectionCount(1)]
    [MaxCollectionCount(20)]
    public List<CreateSegmentDTO> Segments { get; set; } = new();
}
