namespace LootNet_API.DTO.Generation.Create;

public class CreateGenerationProfileFullDTO
{
    public required string Name { get; set; }

    public List<CreateTypeWeightDTO> TypeWeights { get; set; } = new();

    public List<CreateRuleFullDTO> Rules { get; set; } = new();
}
