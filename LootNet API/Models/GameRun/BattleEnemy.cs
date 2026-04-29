namespace LootNet_API.Models.GameRun;

public class BattleEnemy
{
    public Guid Id { get; set; }

    public Guid BattleId { get; set; }

    public int Position { get; set; }

    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }

    public bool IsDisorganized { get; set; }
    public bool SkipNextTurn { get; set; }

    public Guid? LeftHandItemId { get; set; }
    public Guid? RightHandItemId { get; set; }

    public required Equipment Equipment { get; set; }
}
