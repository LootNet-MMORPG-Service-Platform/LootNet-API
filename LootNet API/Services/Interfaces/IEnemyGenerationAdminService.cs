using LootNet_API.DTO.EnemyGeneration.Create;
using LootNet_API.DTO.EnemyGeneration.Response;
using LootNet_API.DTO.EnemyGeneration.Update;

namespace LootNet_API.Services.Interfaces;

public interface IEnemyGenerationAdminService
{
    Task<Guid> CreateStageProfileAsync(CreateStageProfileDTO dto, Guid adminId = default);
    Task<List<StageProfileDTO>> GetStageProfilesAsync();
    Task UpdateStageProfileAsync(UpdateStageProfileDTO dto, Guid adminId = default);
    Task DeleteStageProfileAsync(Guid id, Guid adminId = default);

    Task<Guid> CreateStageScenarioAsync(Guid stageProfileId, CreateStageScenarioDTO dto, Guid adminId = default);
    Task<List<StageScenarioDTO>> GetStageScenariosAsync(Guid stageProfileId);
    Task UpdateStageScenarioAsync(UpdateStageScenarioDTO dto, Guid adminId = default);
    Task DeleteStageScenarioAsync(Guid id, Guid adminId = default);

    Task<Guid> CreateScenarioSlotAsync(Guid scenarioId, CreateScenarioSlotDTO dto, Guid adminId = default);
    Task<List<ScenarioSlotDTO>> GetScenarioSlotsAsync(Guid scenarioId);
    Task UpdateScenarioSlotAsync(UpdateScenarioSlotDTO dto, Guid adminId = default);
    Task DeleteScenarioSlotAsync(Guid id, Guid adminId = default);

    Task<Guid> CreateEnemyClassProfileAsync(CreateEnemyClassProfileDTO dto, Guid adminId = default);
    Task<List<EnemyClassProfileDTO>> GetEnemyClassProfilesAsync();
    Task UpdateEnemyClassProfileAsync(UpdateEnemyClassProfileDTO dto, Guid adminId = default);
    Task DeleteEnemyClassProfileAsync(Guid id, Guid adminId = default);
}
