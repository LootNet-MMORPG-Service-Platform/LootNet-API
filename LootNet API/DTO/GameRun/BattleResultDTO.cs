namespace LootNet_API.DTO.GameRun;

public class BattleResultDTO
{
    public int DamageDealt { get; set; }
    public int EnemyDamage { get; set; }
    public bool PlayerSkipped { get; set; }
    public bool RunFinished { get; set; }
    public List<string> Log { get; set; } = new();
}