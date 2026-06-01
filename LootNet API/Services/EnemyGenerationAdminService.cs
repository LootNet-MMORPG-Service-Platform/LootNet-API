using LootNet_API.Data;
using LootNet_API.DTO.EnemyGeneration.Create;
using LootNet_API.DTO.EnemyGeneration.Response;
using LootNet_API.DTO.EnemyGeneration.Update;
using LootNet_API.Models.GameRun.EnemyGeneration;
using LootNet_API.Models.Logs;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LootNet_API.Services;

public class EnemyGenerationAdminService : IEnemyGenerationAdminService
{
    private readonly AppDbContext _context;
    private readonly IRealtimeNotifier? _realtimeNotifier;

    public EnemyGenerationAdminService(AppDbContext context, IRealtimeNotifier? realtimeNotifier = null)
    {
        _context = context;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<Guid> CreateStageProfileAsync(CreateStageProfileDTO dto, Guid adminId = default)
    {
        var profile = new StageProfile
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            StageIndex = dto.StageIndex,
            Weight = dto.Weight,
            Falloff = dto.Falloff,
            Threshold = dto.Threshold
        };

        _context.StageProfiles.Add(profile);
        await _context.SaveChangesAsync();
        await LogAsync(adminId, "CREATE_STAGE_PROFILE", profile.Id, "create-stage-profile");

        return profile.Id;
    }

    public async Task<List<StageProfileDTO>> GetStageProfilesAsync()
        => await _context.StageProfiles
            .OrderBy(x => x.StageIndex)
            .Select(x => new StageProfileDTO
            {
                Id = x.Id,
                Name = x.Name,
                StageIndex = x.StageIndex,
                Weight = x.Weight,
                Falloff = x.Falloff,
                Threshold = x.Threshold
            })
            .ToListAsync();

    public async Task UpdateStageProfileAsync(UpdateStageProfileDTO dto, Guid adminId = default)
    {
        var profile = await _context.StageProfiles.FirstAsync(x => x.Id == dto.Id);

        profile.Name = dto.Name;
        profile.StageIndex = dto.StageIndex;
        profile.Weight = dto.Weight;
        profile.Falloff = dto.Falloff;
        profile.Threshold = dto.Threshold;

        await _context.SaveChangesAsync();
        await LogAsync(adminId, "UPDATE_STAGE_PROFILE", dto.Id, "update-stage-profile");
    }

    public async Task DeleteStageProfileAsync(Guid id, Guid adminId = default)
    {
        _context.StageProfiles.Remove(await _context.StageProfiles.FirstAsync(x => x.Id == id));
        await _context.SaveChangesAsync();
        await LogAsync(adminId, "DELETE_STAGE_PROFILE", id, "delete-stage-profile");
    }

    public async Task<Guid> CreateStageScenarioAsync(Guid stageProfileId, CreateStageScenarioDTO dto, Guid adminId = default)
    {
        var scenario = new StageScenario
        {
            Id = Guid.NewGuid(),
            StageProfileId = stageProfileId,
            EnemyCount = dto.EnemyCount,
            Weight = dto.Weight
        };

        _context.StageScenarios.Add(scenario);
        await _context.SaveChangesAsync();
        await LogAsync(adminId, "CREATE_STAGE_SCENARIO", scenario.Id, "create-stage-scenario", new { scenario.Id, stageProfileId });

        return scenario.Id;
    }

    public async Task<List<StageScenarioDTO>> GetStageScenariosAsync(Guid stageProfileId)
        => await _context.StageScenarios
            .Where(x => x.StageProfileId == stageProfileId)
            .Select(x => new StageScenarioDTO
            {
                Id = x.Id,
                StageProfileId = x.StageProfileId,
                EnemyCount = x.EnemyCount,
                Weight = x.Weight
            })
            .ToListAsync();

    public async Task UpdateStageScenarioAsync(UpdateStageScenarioDTO dto, Guid adminId = default)
    {
        var scenario = await _context.StageScenarios.FirstAsync(x => x.Id == dto.Id);

        scenario.EnemyCount = dto.EnemyCount;
        scenario.Weight = dto.Weight;

        await _context.SaveChangesAsync();
        await LogAsync(adminId, "UPDATE_STAGE_SCENARIO", dto.Id, "update-stage-scenario");
    }

    public async Task DeleteStageScenarioAsync(Guid id, Guid adminId = default)
    {
        _context.StageScenarios.Remove(await _context.StageScenarios.FirstAsync(x => x.Id == id));
        await _context.SaveChangesAsync();
        await LogAsync(adminId, "DELETE_STAGE_SCENARIO", id, "delete-stage-scenario");
    }

    public async Task<Guid> CreateScenarioSlotAsync(Guid scenarioId, CreateScenarioSlotDTO dto, Guid adminId = default)
    {
        var slot = new ScenarioSlot
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            Position = dto.Position,
            ClassProfileId = dto.ClassProfileId,
            Weight = dto.Weight
        };

        _context.ScenarioSlots.Add(slot);
        await _context.SaveChangesAsync();
        await LogAsync(adminId, "CREATE_SCENARIO_SLOT", slot.Id, "create-scenario-slot", new { slot.Id, scenarioId });

        return slot.Id;
    }

    public async Task<List<ScenarioSlotDTO>> GetScenarioSlotsAsync(Guid scenarioId)
        => await _context.ScenarioSlots
            .Where(x => x.ScenarioId == scenarioId)
            .Select(x => new ScenarioSlotDTO
            {
                Id = x.Id,
                ScenarioId = x.ScenarioId,
                Position = x.Position,
                ClassProfileId = x.ClassProfileId,
                Weight = x.Weight
            })
            .ToListAsync();

    public async Task UpdateScenarioSlotAsync(UpdateScenarioSlotDTO dto, Guid adminId = default)
    {
        var slot = await _context.ScenarioSlots.FirstAsync(x => x.Id == dto.Id);

        slot.Position = dto.Position;
        slot.ClassProfileId = dto.ClassProfileId;
        slot.Weight = dto.Weight;

        await _context.SaveChangesAsync();
        await LogAsync(adminId, "UPDATE_SCENARIO_SLOT", dto.Id, "update-scenario-slot");
    }

    public async Task DeleteScenarioSlotAsync(Guid id, Guid adminId = default)
    {
        _context.ScenarioSlots.Remove(await _context.ScenarioSlots.FirstAsync(x => x.Id == id));
        await _context.SaveChangesAsync();
        await LogAsync(adminId, "DELETE_SCENARIO_SLOT", id, "delete-scenario-slot");
    }

    public async Task<Guid> CreateEnemyClassProfileAsync(CreateEnemyClassProfileDTO dto, Guid adminId = default)
    {
        var profile = new EnemyClassProfile
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Class = dto.Class,
            AllowedColumns = dto.AllowedColumns,
            GenerationProfileId = dto.GenerationProfileId,
            Weight = dto.Weight
        };

        _context.EnemyClassProfiles.Add(profile);
        await _context.SaveChangesAsync();
        await LogAsync(adminId, "CREATE_CLASS_PROFILE", profile.Id, "create-class-profile");

        return profile.Id;
    }

    public async Task<List<EnemyClassProfileDTO>> GetEnemyClassProfilesAsync()
        => await _context.EnemyClassProfiles
            .Select(x => new EnemyClassProfileDTO
            {
                Id = x.Id,
                Name = x.Name,
                Class = x.Class,
                AllowedColumns = x.AllowedColumns,
                GenerationProfileId = x.GenerationProfileId,
                Weight = x.Weight
            })
            .ToListAsync();

    public async Task UpdateEnemyClassProfileAsync(UpdateEnemyClassProfileDTO dto, Guid adminId = default)
    {
        var profile = await _context.EnemyClassProfiles.FirstAsync(x => x.Id == dto.Id);

        profile.Name = dto.Name;
        profile.Class = dto.Class;
        profile.AllowedColumns = dto.AllowedColumns;
        profile.GenerationProfileId = dto.GenerationProfileId;
        profile.Weight = dto.Weight;

        await _context.SaveChangesAsync();
        await LogAsync(adminId, "UPDATE_CLASS_PROFILE", dto.Id, "update-class-profile");
    }

    public async Task DeleteEnemyClassProfileAsync(Guid id, Guid adminId = default)
    {
        _context.EnemyClassProfiles.Remove(await _context.EnemyClassProfiles.FirstAsync(x => x.Id == id));
        await _context.SaveChangesAsync();
        await LogAsync(adminId, "DELETE_CLASS_PROFILE", id, "delete-class-profile");
    }

    private async Task LogAsync(Guid adminId, string action, Guid id, string notificationAction, object? notificationData = null)
    {
        _context.AdminLogs.Add(new AdminLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            TargetUserId = id.ToString(),
            AdminId = adminId
        });

        await _context.SaveChangesAsync();
        await NotifyAsync("enemy-generation", notificationAction, null, notificationData ?? new { id });
    }

    private Task NotifyAsync(string domain, string action, Guid? userId = null, object? data = null)
        => _realtimeNotifier?.AppChangedAsync(domain, action, userId, data) ?? Task.CompletedTask;
}
