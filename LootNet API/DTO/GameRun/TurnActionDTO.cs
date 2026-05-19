using System.ComponentModel.DataAnnotations;
using LootNet_API.Enums;
using LootNet_API.Models.Items;

namespace LootNet_API.DTO.GameRun;

public class TurnActionDTO
{
    public ActionType Type { get; set; }

    [Range(0, 4)]
    public int TargetPosition { get; set; }

    public Weapon? LeftWeapon { get; set; }

    public Weapon? RightWeapon { get; set; }
}
