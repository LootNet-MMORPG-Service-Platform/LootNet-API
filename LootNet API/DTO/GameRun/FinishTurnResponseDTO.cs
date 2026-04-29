namespace LootNet_API.DTO.GameRun;

public class FinishTurnResponseDTO
{
    public List<TurnActionDTO> Actions { get; set; } = new();
    public BattleResponseDTO? Battle { get; set; }
    public RunResponseDTO? Run { get; set; }
}