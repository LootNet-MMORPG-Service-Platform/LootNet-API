using LootNet_API.DTO.Generation.Create;
using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Update;

public class UpdateElementDTO
{
    public Guid Id { get; set; }
    public ItemElementType ElementType { get; set; }
    public List<CreateSegmentDTO> Segments { get; set; } = new();
}
