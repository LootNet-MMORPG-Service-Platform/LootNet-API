namespace LootNet_API.Models.GameRun.EnemyGeneration;

public class StageScenario
{
    public Guid Id { get; set; }

    public Guid StageProfileId { get; set; }

    public int EnemyCount { get; set; }

    public double Weight { get; set; }

    public List<ScenarioSlot> Slots { get; set; } = new();
}
