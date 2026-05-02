namespace LootNet_API.DTO.GameRun;

public class BattleDTO
{
    public Guid BattleId { get; set; }
    public Guid RunId { get; set; }
    public int PlayerCurrentHp { get; set; }
    public int PlayerMaxHp { get; set; }
    public int PlayerPosition { get; set; }
    public Guid? LeftHandItemId { get; set; }
    public Guid? RightHandItemId { get; set; }
    public required List<BattleEnemyDTO> Enemies { get; set; }
}
