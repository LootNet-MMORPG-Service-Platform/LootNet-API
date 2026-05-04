using LootNet_API.Enums;

namespace LootNet_API.DTO.EnemyGeneration.Response;

public class EnemyClassProfileDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public EnemyClass Class { get; set; }
    public List<int> AllowedColumns { get; set; } = new();
    public Guid GenerationProfileId { get; set; }
    public double Weight { get; set; }
}
