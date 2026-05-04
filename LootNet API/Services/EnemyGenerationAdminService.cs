using LootNet_API.Data;
using LootNet_API.DTO.EnemyGeneration.Create;
using LootNet_API.DTO.EnemyGeneration.Response;
using LootNet_API.DTO.EnemyGeneration.Update;
using LootNet_API.Models.GameRun.EnemyGeneration;
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

    public async Task<Guid> CreateStageProfileAsync(CreateStageProfileDTO dto)
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
        await NotifyAsync("enemy-generation", "create-stage-profile", null, new { profile.Id });

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

    public async Task UpdateStageProfileAsync(UpdateStageProfileDTO dto)
    {
        var profile = await _context.StageProfiles.FirstAsync(x => x.Id == dto.Id);

        profile.Name = dto.Name;
        profile.StageIndex = dto.StageIndex;
        profile.Weight = dto.Weight;
        profile.Falloff = dto.Falloff;
        profile.Threshold = dto.Threshold;

        await _context.SaveChangesAsync();
        await NotifyAsync("enemy-generation", "update-stage-profile", null, new { dto.Id });
    }

    public async Task DeleteStageProfileAsync(Guid id)
    {
        _context.StageProfiles.Remove(await _context.StageProfiles.FirstAsync(x => x.Id == id));
        await _context.SaveChangesAsync();
        await NotifyAsync("enemy-generation", "delete-stage-profile", null, new { id });
    }

    public async Task<Guid> CreateStageScenarioAsync(Guid stageProfileId, CreateStageScenarioDTO dto)
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
        await NotifyAsync("enemy-generation", "create-stage-scenario", null, new { scenario.Id, stageProfileId });

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

    public async Task UpdateStageScenarioAsync(UpdateStageScenarioDTO dto)
    {
        var scenario = await _context.StageScenarios.FirstAsync(x => x.Id == dto.Id);

        scenario.EnemyCount = dto.EnemyCount;
        scenario.Weight = dto.Weight;

        await _context.SaveChangesAsync();
        await NotifyAsync("enemy-generation", "update-stage-scenario", null, new { dto.Id });
    }

    public async Task DeleteStageScenarioAsync(Guid id)
    {
        _context.StageScenarios.Remove(await _context.StageScenarios.FirstAsync(x => x.Id == id));
        await _context.SaveChangesAsync();
        await NotifyAsync("enemy-generation", "delete-stage-scenario", null, new { id });
    }

    public async Task<Guid> CreateScenarioSlotAsync(Guid scenarioId, CreateScenarioSlotDTO dto)
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
        await NotifyAsync("enemy-generation", "create-scenario-slot", null, new { slot.Id, scenarioId });

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

    public async Task UpdateScenarioSlotAsync(UpdateScenarioSlotDTO dto)
    {
        var slot = await _context.ScenarioSlots.FirstAsync(x => x.Id == dto.Id);

        slot.Position = dto.Position;
        slot.ClassProfileId = dto.ClassProfileId;
        slot.Weight = dto.Weight;

        await _context.SaveChangesAsync();
        await NotifyAsync("enemy-generation", "update-scenario-slot", null, new { dto.Id });
    }

    public async Task DeleteScenarioSlotAsync(Guid id)
    {
        _context.ScenarioSlots.Remove(await _context.ScenarioSlots.FirstAsync(x => x.Id == id));
        await _context.SaveChangesAsync();
        await NotifyAsync("enemy-generation", "delete-scenario-slot", null, new { id });
    }

    public async Task<Guid> CreateEnemyClassProfileAsync(CreateEnemyClassProfileDTO dto)
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
        await NotifyAsync("enemy-generation", "create-class-profile", null, new { profile.Id });

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

    public async Task UpdateEnemyClassProfileAsync(UpdateEnemyClassProfileDTO dto)
    {
        var profile = await _context.EnemyClassProfiles.FirstAsync(x => x.Id == dto.Id);

        profile.Name = dto.Name;
        profile.Class = dto.Class;
        profile.AllowedColumns = dto.AllowedColumns;
        profile.GenerationProfileId = dto.GenerationProfileId;
        profile.Weight = dto.Weight;

        await _context.SaveChangesAsync();
        await NotifyAsync("enemy-generation", "update-class-profile", null, new { dto.Id });
    }

    public async Task DeleteEnemyClassProfileAsync(Guid id)
    {
        _context.EnemyClassProfiles.Remove(await _context.EnemyClassProfiles.FirstAsync(x => x.Id == id));
        await _context.SaveChangesAsync();
        await NotifyAsync("enemy-generation", "delete-class-profile", null, new { id });
    }

    private Task NotifyAsync(string domain, string action, Guid? userId = null, object? data = null)
        => _realtimeNotifier?.AppChangedAsync(domain, action, userId, data) ?? Task.CompletedTask;
}
