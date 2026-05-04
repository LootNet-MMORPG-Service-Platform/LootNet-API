using LootNet_API.Enums;

namespace LootNet_API.DTO.Admin;

public class AdminRunListDTO
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public RunStatus Status { get; set; }
    public int BattleIndex { get; set; }
    public int PlayerCurrentHp { get; set; }
    public int PlayerMaxHp { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
