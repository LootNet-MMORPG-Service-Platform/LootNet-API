using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Response;

public class ElementDTO
{
    public Guid Id { get; set; }
    public ItemElementType ElementType { get; set; }
    public List<SegmentDTO> Segments { get; set; } = new();
}
