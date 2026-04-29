namespace LootNet_API.Models.GameRun.EnemyGeneration;

public class StageProfile
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int StageIndex { get; set; }

    public double Weight { get; set; }

    public double Falloff { get; set; }
    public int Threshold { get; set; } = 1;

    public List<StageScenario> Scenarios { get; set; } = new();
}
