namespace LootNet_API.Models.GameRun.EnemyGeneration;

using Enums;

public class EnemyClassProfile
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public EnemyClass Class { get; set; }

    public List<int> AllowedColumns { get; set; } = new();

    public Guid GenerationProfileId { get; set; }

    public double Weight { get; set; }
}