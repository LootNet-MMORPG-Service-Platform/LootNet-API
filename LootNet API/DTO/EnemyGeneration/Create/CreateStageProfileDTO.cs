using System.ComponentModel.DataAnnotations;

namespace LootNet_API.DTO.EnemyGeneration.Create;

public class CreateStageProfileDTO
{
    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 1000)]
    public int StageIndex { get; set; }

    [Range(0.0001, 1000000)]
    public double Weight { get; set; }

    [Range(0, 1000000)]
    public double Falloff { get; set; }

    [Range(1, 1000)]
    public int Threshold { get; set; } = 1;
}

