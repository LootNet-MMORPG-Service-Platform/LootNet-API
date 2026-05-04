using LootNet_API.Enums;

namespace LootNet_API.DTO.Admin;

public class AdminRunDetailsDTO
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public RunStatus Status { get; set; }
    public int BattleIndex { get; set; }
    public int PlayerCurrentHp { get; set; }
    public int PlayerMaxHp { get; set; }
    public bool IsPlayerDisorganized { get; set; }
    public bool PlayerSkipNextTurn { get; set; }
    public int PlayerPosition { get; set; }
    public Guid? LeftHandItemId { get; set; }
    public Guid? RightHandItemId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public List<AdminBattleDTO> Battles { get; set; } = new();
}
