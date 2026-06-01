namespace LootNet_API.Tests;

using System.Security.Claims;
using LootNet_API.Controllers;
using LootNet_API.Data;
using LootNet_API.DTO.EnemyGeneration.Create;
using LootNet_API.DTO.EnemyGeneration.Update;
using LootNet_API.Enums;
using LootNet_API.Models.GameRun.EnemyGeneration;
using LootNet_API.Models.Items.Generation;
using LootNet_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class EnemyGenerationAdminServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static EnemyGenerationAdminService CreateService(AppDbContext db)
        => new EnemyGenerationAdminService(db);

    [Fact]
    public async Task CreateStageProfile_ShouldPersist()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var id = await service.CreateStageProfileAsync(new CreateStageProfileDTO
        {
            Name = "Stage A",
            StageIndex = 1,
            Weight = 1.5,
            Falloff = 0.2,
            Threshold = 3
        });

        var profile = await db.StageProfiles.FirstAsync(x => x.Id == id);

        Assert.Equal("Stage A", profile.Name);
        Assert.Equal(1, profile.StageIndex);
        Assert.Equal(1.5, profile.Weight);
        Assert.Equal(0.2, profile.Falloff);
        Assert.Equal(3, profile.Threshold);

        var log = await db.AdminLogs.SingleAsync();
        Assert.Equal("CREATE_STAGE_PROFILE", log.Action);
        Assert.Equal(id.ToString(), log.TargetUserId);
    }

    [Fact]
    public async Task GetStageProfiles_ShouldReturnOrderedByStageIndex()
    {
        using var db = CreateDb();
        db.StageProfiles.Add(new StageProfile { Id = Guid.NewGuid(), Name = "B", StageIndex = 2, Weight = 1, Falloff = 0, Threshold = 1 });
        db.StageProfiles.Add(new StageProfile { Id = Guid.NewGuid(), Name = "A", StageIndex = 1, Weight = 1, Falloff = 0, Threshold = 1 });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.GetStageProfilesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].StageIndex);
        Assert.Equal(2, result[1].StageIndex);
    }

    [Fact]
    public async Task UpdateStageProfile_ShouldChangeValues()
    {
        using var db = CreateDb();
        var profile = new StageProfile { Id = Guid.NewGuid(), Name = "Old", StageIndex = 1, Weight = 1, Falloff = 0, Threshold = 1 };
        db.StageProfiles.Add(profile);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await service.UpdateStageProfileAsync(new UpdateStageProfileDTO
        {
            Id = profile.Id,
            Name = "New",
            StageIndex = 5,
            Weight = 2,
            Falloff = 0.7,
            Threshold = 9
        });

        var updated = await db.StageProfiles.FirstAsync(x => x.Id == profile.Id);

        Assert.Equal("New", updated.Name);
        Assert.Equal(5, updated.StageIndex);
        Assert.Equal(2, updated.Weight);
        Assert.Equal(0.7, updated.Falloff);
        Assert.Equal(9, updated.Threshold);

        var log = await db.AdminLogs.SingleAsync();
        Assert.Equal("UPDATE_STAGE_PROFILE", log.Action);
        Assert.Equal(profile.Id.ToString(), log.TargetUserId);
    }

    [Fact]
    public async Task DeleteStageProfile_ShouldRemoveEntity()
    {
        using var db = CreateDb();
        var profile = new StageProfile { Id = Guid.NewGuid(), Name = "Delete", StageIndex = 3, Weight = 1, Falloff = 0, Threshold = 1 };
        db.StageProfiles.Add(profile);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await service.DeleteStageProfileAsync(profile.Id);

        Assert.Empty(db.StageProfiles);

        var log = await db.AdminLogs.SingleAsync();
        Assert.Equal("DELETE_STAGE_PROFILE", log.Action);
        Assert.Equal(profile.Id.ToString(), log.TargetUserId);
    }

    [Fact]
    public async Task ScenarioCrud_ShouldCreateReadUpdateDelete()
    {
        using var db = CreateDb();
        var stage = new StageProfile { Id = Guid.NewGuid(), Name = "S", StageIndex = 1, Weight = 1, Falloff = 0, Threshold = 1 };
        db.StageProfiles.Add(stage);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var scenarioId = await service.CreateStageScenarioAsync(stage.Id, new CreateStageScenarioDTO
        {
            EnemyCount = 2,
            Weight = 3.4
        });

        var list = await service.GetStageScenariosAsync(stage.Id);
        Assert.Single(list);
        Assert.Equal(scenarioId, list[0].Id);

        await service.UpdateStageScenarioAsync(new UpdateStageScenarioDTO
        {
            Id = scenarioId,
            EnemyCount = 4,
            Weight = 9
        });

        var updated = await db.StageScenarios.FirstAsync(x => x.Id == scenarioId);
        Assert.Equal(4, updated.EnemyCount);
        Assert.Equal(9, updated.Weight);

        await service.DeleteStageScenarioAsync(scenarioId);
        Assert.Empty(db.StageScenarios);

        var scenarioActions = await db.AdminLogs.Select(x => x.Action).ToListAsync();
        Assert.Equal(3, scenarioActions.Count);
        Assert.Contains("CREATE_STAGE_SCENARIO", scenarioActions);
        Assert.Contains("UPDATE_STAGE_SCENARIO", scenarioActions);
        Assert.Contains("DELETE_STAGE_SCENARIO", scenarioActions);
    }

    [Fact]
    public async Task SlotCrud_ShouldCreateReadUpdateDelete()
    {
        using var db = CreateDb();

        var generationProfile = new GenerationProfile { Id = Guid.NewGuid(), Name = "GP" };
        db.GenerationProfiles.Add(generationProfile);

        var classProfile = new EnemyClassProfile
        {
            Id = Guid.NewGuid(),
            Name = "Tank A",
            Class = EnemyClass.Tank,
            AllowedColumns = new List<int> { 1 },
            GenerationProfileId = generationProfile.Id,
            Weight = 1
        };
        db.EnemyClassProfiles.Add(classProfile);

        var stage = new StageProfile { Id = Guid.NewGuid(), Name = "S", StageIndex = 1, Weight = 1, Falloff = 0, Threshold = 1 };
        var scenario = new StageScenario { Id = Guid.NewGuid(), StageProfileId = stage.Id, EnemyCount = 1, Weight = 1 };
        db.StageProfiles.Add(stage);
        db.StageScenarios.Add(scenario);

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var slotId = await service.CreateScenarioSlotAsync(scenario.Id, new CreateScenarioSlotDTO
        {
            Position = 2,
            ClassProfileId = classProfile.Id,
            Weight = 5
        });

        var slots = await service.GetScenarioSlotsAsync(scenario.Id);
        Assert.Single(slots);
        Assert.Equal(slotId, slots[0].Id);

        await service.UpdateScenarioSlotAsync(new UpdateScenarioSlotDTO
        {
            Id = slotId,
            Position = 3,
            ClassProfileId = classProfile.Id,
            Weight = 8
        });

        var updated = await db.ScenarioSlots.FirstAsync(x => x.Id == slotId);
        Assert.Equal(3, updated.Position);
        Assert.Equal(8, updated.Weight);

        await service.DeleteScenarioSlotAsync(slotId);
        Assert.Empty(db.ScenarioSlots);

        var slotActions = await db.AdminLogs.Select(x => x.Action).ToListAsync();
        Assert.Equal(3, slotActions.Count);
        Assert.Contains("CREATE_SCENARIO_SLOT", slotActions);
        Assert.Contains("UPDATE_SCENARIO_SLOT", slotActions);
        Assert.Contains("DELETE_SCENARIO_SLOT", slotActions);
    }

    [Fact]
    public async Task EnemyClassProfileCrud_ShouldCreateReadUpdateDelete()
    {
        using var db = CreateDb();

        var generationProfile = new GenerationProfile { Id = Guid.NewGuid(), Name = "GP" };
        db.GenerationProfiles.Add(generationProfile);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var id = await service.CreateEnemyClassProfileAsync(new CreateEnemyClassProfileDTO
        {
            Name = "Archer A",
            Class = EnemyClass.Archer,
            AllowedColumns = new List<int> { 3, 4 },
            GenerationProfileId = generationProfile.Id,
            Weight = 1.2
        });

        var all = await service.GetEnemyClassProfilesAsync();
        Assert.Single(all);
        Assert.Equal(id, all[0].Id);

        await service.UpdateEnemyClassProfileAsync(new UpdateEnemyClassProfileDTO
        {
            Id = id,
            Name = "Archer B",
            Class = EnemyClass.Crossbow,
            AllowedColumns = new List<int> { 2, 3 },
            GenerationProfileId = generationProfile.Id,
            Weight = 7
        });

        var updated = await db.EnemyClassProfiles.FirstAsync(x => x.Id == id);
        Assert.Equal("Archer B", updated.Name);
        Assert.Equal(EnemyClass.Crossbow, updated.Class);
        Assert.Equal(7, updated.Weight);
        Assert.Equal(2, updated.AllowedColumns.Count);

        await service.DeleteEnemyClassProfileAsync(id);
        Assert.Empty(db.EnemyClassProfiles);

        var classProfileActions = await db.AdminLogs.Select(x => x.Action).ToListAsync();
        Assert.Equal(3, classProfileActions.Count);
        Assert.Contains("CREATE_CLASS_PROFILE", classProfileActions);
        Assert.Contains("UPDATE_CLASS_PROFILE", classProfileActions);
        Assert.Contains("DELETE_CLASS_PROFILE", classProfileActions);
    }

    [Fact]
    public async Task EnemyGenerationController_UpdateStageProfile_LogsCurrentAdminId()
    {
        using var db = CreateDb();
        var adminId = Guid.NewGuid();
        var profile = new StageProfile
        {
            Id = Guid.NewGuid(),
            Name = "Old",
            StageIndex = 1,
            Weight = 1,
            Falloff = 0,
            Threshold = 1
        };
        db.StageProfiles.Add(profile);
        await db.SaveChangesAsync();

        var controller = CreateController(db, adminId);

        var result = await controller.UpdateStageProfile(new UpdateStageProfileDTO
        {
            Id = profile.Id,
            Name = "New",
            StageIndex = 2,
            Weight = 3,
            Falloff = 0.4,
            Threshold = 5
        });

        Assert.IsType<OkResult>(result);

        var log = await db.AdminLogs.SingleAsync();
        Assert.Equal(adminId, log.AdminId);
        Assert.Equal("UPDATE_STAGE_PROFILE", log.Action);
        Assert.Equal(profile.Id.ToString(), log.TargetUserId);
    }

    [Fact]
    public async Task UpdateStageProfile_WhenMissing_ShouldThrow()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStageProfileAsync(new UpdateStageProfileDTO
            {
                Id = Guid.NewGuid(),
                Name = "X",
                StageIndex = 1,
                Weight = 1,
                Falloff = 0,
                Threshold = 1
            }));
    }

    [Fact]
    public async Task DeleteStageScenario_WhenMissing_ShouldThrow()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteStageScenarioAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateScenarioSlot_WhenMissing_ShouldThrow()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateScenarioSlotAsync(new UpdateScenarioSlotDTO
            {
                Id = Guid.NewGuid(),
                Position = 1,
                ClassProfileId = Guid.NewGuid(),
                Weight = 1
            }));
    }

    [Fact]
    public async Task DeleteEnemyClassProfile_WhenMissing_ShouldThrow()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteEnemyClassProfileAsync(Guid.NewGuid()));
    }

    private static EnemyGenerationAdminController CreateController(AppDbContext db, Guid adminId)
    {
        return new EnemyGenerationAdminController(new EnemyGenerationAdminService(db))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, adminId.ToString())
                    }))
                }
            }
        };
    }
}
