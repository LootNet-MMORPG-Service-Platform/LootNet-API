using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Response;

public class ParameterDTO
{
    public Guid Id { get; set; }
    public ItemParameter Parameter { get; set; }
    public List<SegmentDTO> Segments { get; set; } = new();
}