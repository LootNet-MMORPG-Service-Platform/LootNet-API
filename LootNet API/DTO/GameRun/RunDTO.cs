using LootNet_API.Enums;

namespace LootNet_API.DTO.GameRun;

public class RunDTO
{
    public Guid Id { get; set; }
    public RunStatus Status { get; set; }
    public int BattleIndex { get; set; }
    public int PlayerCurrentHp { get; set; }
    public int PlayerMaxHp { get; set; }
}