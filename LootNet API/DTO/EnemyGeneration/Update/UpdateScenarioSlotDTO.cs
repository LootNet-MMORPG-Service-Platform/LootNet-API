namespace LootNet_API.DTO.EnemyGeneration.Update;

public class UpdateScenarioSlotDTO
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public Guid ClassProfileId { get; set; }
    public double Weight { get; set; }
}
