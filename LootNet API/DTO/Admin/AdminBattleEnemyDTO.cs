using LootNet_API.Enums;

namespace LootNet_API.DTO.Admin;

public class AdminBattleEnemyDTO
{
    public Guid Id { get; set; }
    public EnemyClass Class { get; set; }
    public int Position { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
}
