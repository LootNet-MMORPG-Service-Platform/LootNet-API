using LootNet_API.Enums;

namespace LootNet_API.DTO.GameRun;

public class BattleEnemyDTO
{
    public Guid Id { get; set; }
    public EnemyClass Class { get; set; }
    public int Position { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public Guid? LeftHandItemId { get; set; }
    public Guid? RightHandItemId { get; set; }
}
