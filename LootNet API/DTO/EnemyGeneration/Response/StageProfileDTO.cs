namespace LootNet_API.DTO.EnemyGeneration.Response;

public class StageProfileDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StageIndex { get; set; }
    public double Weight { get; set; }
    public double Falloff { get; set; }
    public int Threshold { get; set; }
}
