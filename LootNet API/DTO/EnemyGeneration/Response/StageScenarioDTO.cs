namespace LootNet_API.DTO.EnemyGeneration.Response;

public class StageScenarioDTO
{
    public Guid Id { get; set; }
    public Guid StageProfileId { get; set; }
    public int EnemyCount { get; set; }
    public double Weight { get; set; }
}
