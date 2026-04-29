namespace LootNet_API.DTO.GameRun;

public class BattleResponseDTO
{
    public Guid RunId { get; set; }
    public Guid BattleId { get; set; }
    public int BattleIndex { get; set; }
    public int PlayerCurrentHp { get; set; }
    public int PlayerMaxHp { get; set; }
    public List<BattleEnemyDTO> Enemies { get; set; } = new();
}
