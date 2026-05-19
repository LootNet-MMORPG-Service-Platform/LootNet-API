using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;

namespace LootNet_API.DTO.EnemyGeneration.Update;

public class UpdateScenarioSlotDTO
{
    [NotEmptyGuid]
    public Guid Id { get; set; }

    [Range(0, 4)]
    public int Position { get; set; }

    [NotEmptyGuid]
    public Guid ClassProfileId { get; set; }

    [Range(0.0001, 1000000)]
    public double Weight { get; set; }
}

