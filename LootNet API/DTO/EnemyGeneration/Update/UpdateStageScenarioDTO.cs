using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;

namespace LootNet_API.DTO.EnemyGeneration.Update;

public class UpdateStageScenarioDTO
{
    [NotEmptyGuid]
    public Guid Id { get; set; }

    [Range(1, 20)]
    public int EnemyCount { get; set; }

    [Range(0.0001, 1000000)]
    public double Weight { get; set; }
}

