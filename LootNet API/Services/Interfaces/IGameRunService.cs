using LootNet_API.DTO.GameRun;

namespace LootNet_API.Services.Interfaces;

public interface IGameRunService
{
    Task<RunDTO?> GetActiveRunAsync(Guid userId);
    Task<RunDTO> StartRunAsync(Guid userId, StartRunDTO dto);
    Task<BattleDTO> GoFurtherAsync(Guid userId);
    Task<BattleResultDTO> FinishTurnAsync(Guid userId, FinishTurnDTO dto);
    Task<RunDTO> EndRunAsync(Guid userId);
    Task<RunDTO> ForceReturnAsync(Guid userId);
}
