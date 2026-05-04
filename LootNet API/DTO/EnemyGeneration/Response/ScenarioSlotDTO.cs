namespace LootNet_API.DTO.EnemyGeneration.Response;

public class ScenarioSlotDTO
{
    public Guid Id { get; set; }
    public Guid ScenarioId { get; set; }
    public int Position { get; set; }
    public Guid ClassProfileId { get; set; }
    public double Weight { get; set; }
}
