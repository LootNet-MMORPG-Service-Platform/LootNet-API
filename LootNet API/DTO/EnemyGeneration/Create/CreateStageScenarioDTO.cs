using System.ComponentModel.DataAnnotations;

namespace LootNet_API.DTO.EnemyGeneration.Create;

public class CreateStageScenarioDTO
{
    [Range(1, 20)]
    public int EnemyCount { get; set; }

    [Range(0.0001, 1000000)]
    public double Weight { get; set; }
}

