using LootNet_API.DTO.EnemyGeneration.Create;
using LootNet_API.DTO.EnemyGeneration.Response;
using LootNet_API.DTO.EnemyGeneration.Update;

namespace LootNet_API.Services.Interfaces;

public interface IEnemyGenerationAdminService
{
    Task<Guid> CreateStageProfileAsync(CreateStageProfileDTO dto);
    Task<List<StageProfileDTO>> GetStageProfilesAsync();
    Task UpdateStageProfileAsync(UpdateStageProfileDTO dto);
    Task DeleteStageProfileAsync(Guid id);

    Task<Guid> CreateStageScenarioAsync(Guid stageProfileId, CreateStageScenarioDTO dto);
    Task<List<StageScenarioDTO>> GetStageScenariosAsync(Guid stageProfileId);
    Task UpdateStageScenarioAsync(UpdateStageScenarioDTO dto);
    Task DeleteStageScenarioAsync(Guid id);

    Task<Guid> CreateScenarioSlotAsync(Guid scenarioId, CreateScenarioSlotDTO dto);
    Task<List<ScenarioSlotDTO>> GetScenarioSlotsAsync(Guid scenarioId);
    Task UpdateScenarioSlotAsync(UpdateScenarioSlotDTO dto);
    Task DeleteScenarioSlotAsync(Guid id);

    Task<Guid> CreateEnemyClassProfileAsync(CreateEnemyClassProfileDTO dto);
    Task<List<EnemyClassProfileDTO>> GetEnemyClassProfilesAsync();
    Task UpdateEnemyClassProfileAsync(UpdateEnemyClassProfileDTO dto);
    Task DeleteEnemyClassProfileAsync(Guid id);
}
