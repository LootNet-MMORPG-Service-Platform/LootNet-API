namespace LootNet_API.DTO.EnemyGeneration.Create;

public class CreateScenarioSlotDTO
{
    public int Position { get; set; }
    public Guid ClassProfileId { get; set; }
    public double Weight { get; set; }
}
