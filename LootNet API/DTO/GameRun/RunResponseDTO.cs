using LootNet_API.Enums;

namespace LootNet_API.DTO.GameRun;

public class RunResponseDTO
{
    public Guid RunId { get; set; }
    public int BattleIndex { get; set; }
    public int PlayerCurrentHp { get; set; }
    public int PlayerMaxHp { get; set; }
    public RunStatus Status { get; set; }
}
