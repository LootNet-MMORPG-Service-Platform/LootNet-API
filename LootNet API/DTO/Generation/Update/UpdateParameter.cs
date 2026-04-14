using LootNet_API.DTO.Generation.Create;
using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Update;

public class UpdateParameterDTO
{
    public Guid Id { get; set; }
    public ItemParameter Parameter { get; set; }
    public List<CreateSegmentDTO> Segments { get; set; } = new();
}
