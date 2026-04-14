namespace LootNet_API.DTO.Generation.Response;

public class GenerationProfileDetailsDTO
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public List<TypeWeightDTO> TypeWeights { get; set; } = new();
    public List<RuleDTO> Rules { get; set; } = new();
}