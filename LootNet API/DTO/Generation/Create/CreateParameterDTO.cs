using LootNet_API.Enums;

namespace LootNet_API.DTO.Generation.Create;

public class CreateParameterDTO
{
    public ItemParameter Parameter { get; set; }
    public List<CreateSegmentDTO> Segments { get; set; } = new();
}

