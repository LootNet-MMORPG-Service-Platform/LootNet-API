using LootNet_API.Enums;
using LootNet_API.DTO.Items;

namespace LootNet_API.DTO.GameRun;

public class BattleResultDTO
{
    public int DamageDealt { get; set; }
    public int EnemyDamage { get; set; }
    public bool PlayerSkipped { get; set; }
    public bool RunFinished { get; set; }
    public List<string> Log { get; set; } = new();

    public RunStatus RunStatus { get; set; }
    public int PlayerCurrentHp { get; set; }
    public int PlayerMaxHp { get; set; }
    public int PlayerPosition { get; set; }
    public Guid BattleId { get; set; }
    public List<BattleEnemyDTO> Enemies { get; set; } = new();
    public bool PlayerDefeated { get; set; }
    public string? Message { get; set; }
    public List<ItemRewardDTO> RewardItems { get; set; } = new();
    public List<Guid> RewardItemIds { get; set; } = new();
}
