namespace LootNet_API.Models.Items.Generation;

public class GenerationProfile
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public List<ItemGenerationRule> Rules { get; set; } = new();

    public List<ItemTypeWeight> TypeWeights { get; set; } = new();
}
