using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Create;

public class CreateElementDTO
{
    public ItemElementType ElementType { get; set; }
    public List<CreateSegmentDTO> Segments { get; set; } = new();
}