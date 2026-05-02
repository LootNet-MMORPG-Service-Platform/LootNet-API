namespace LootNet_API.DTO.GameRun;

public class FinishTurnDTO
{
    public Guid BattleId { get; set; }
    public required TurnActionDTO Action { get; set; }
}
