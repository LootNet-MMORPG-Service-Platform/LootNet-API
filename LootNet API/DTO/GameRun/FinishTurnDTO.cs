using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;

namespace LootNet_API.DTO.GameRun;

public class FinishTurnDTO
{
    [NotEmptyGuid]
    public Guid BattleId { get; set; }

    [Required]
    public required TurnActionDTO Action { get; set; }
}

