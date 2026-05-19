using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Generation.Create;
using LootNet_API.DTO.Validation;
using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Update;

public class UpdateParameterDTO
{
    [NotEmptyGuid]
    public Guid Id { get; set; }

    public ItemParameter Parameter { get; set; }

    [Required]
    [MinCollectionCount(1)]
    [MaxCollectionCount(20)]
    public List<CreateSegmentDTO> Segments { get; set; } = new();
}
