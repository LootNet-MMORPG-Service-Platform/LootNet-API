namespace LootNet_API.Models.GameRun.EnemyGeneration;

public class ScenarioSlot
{
    public Guid Id { get; set; }

    public Guid ScenarioId { get; set; }

    public int Position { get; set; }

    public Guid ClassProfileId { get; set; }

    public double Weight { get; set; }
}
