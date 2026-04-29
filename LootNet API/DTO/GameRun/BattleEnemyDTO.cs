namespace LootNet_API.DTO.GameRun;

public class BattleEnemyDTO
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
}