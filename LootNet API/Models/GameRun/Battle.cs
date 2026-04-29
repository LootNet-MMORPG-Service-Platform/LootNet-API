namespace LootNet_API.Models.GameRun;

public class Battle
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }

    public required List<BattleEnemy> Enemies { get; set; }
}
