using LootNet_API.Data;
using LootNet_API.Enums;
using LootNet_API.Models.GameRun;
using LootNet_API.Models.GameRun.EnemyGeneration;
using LootNet_API.Models.Items;
using LootNet_API.Models.Items.Generation;
using LootNet_API.Services;
using LootNet_API.Services.Interfaces;
using LootNet_API.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LootNet_API.Tests;

public class EnemyGenerationServiceTests
{
    private (AppDbContext db, SingleContextFactory factory) Create()
        => DbHelper.Create();

    private EnemyGenerationService CreateService(
        AppDbContext db,
        Mock<IItemGenerationService>? itemGen = null,
        Mock<IEquipmentService>? equipmentSvc = null)
    {
        var mockItemGen = itemGen ?? CreateDefaultItemGen();
        var mockEquipment = equipmentSvc ?? CreateDefaultEquipmentSvc();
        return new EnemyGenerationService(db, mockItemGen.Object, mockEquipment.Object);
    }

    private Mock<IItemGenerationService> CreateDefaultItemGen()
    {
        var mock = new Mock<IItemGenerationService>();
        mock.Setup(x => x.GenerateForEnemyAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => new List<Item>
            {
                new Weapon
                {
                    Id         = Guid.NewGuid(),
                    Name       = "Sword",
                    Category   = ItemCategory.Weapon,
                    WeaponType = WeaponType.Sword,
                    Cut        = 10,
                    Elements   = new List<ItemElement>()
                }
            });
        return mock;
    }

    private Mock<IEquipmentService> CreateDefaultEquipmentSvc()
    {
        var mock = new Mock<IEquipmentService>();
        mock.Setup(x => x.ApplyEnemyEquipment(It.IsAny<Equipment>(), It.IsAny<List<Item>>()));
        return mock;
    }

    private async Task<Guid> SeedStageAsync(
        AppDbContext db,
        List<(EnemyClass cls, int position)> slots,
        int stageIndex = 0)
    {
        var profileId = Guid.NewGuid();

        var classProfiles = slots.Select(s => new EnemyClassProfile
        {
            Id = Guid.NewGuid(),
            Name = s.cls.ToString(),
            Class = s.cls,
            GenerationProfileId = profileId,
            Weight = 1
        }).ToList();

        db.Set<EnemyClassProfile>().AddRange(classProfiles);

        var scenarioSlots = classProfiles.Zip(slots, (cp, s) => new ScenarioSlot
        {
            Id = Guid.NewGuid(),
            ClassProfileId = cp.Id,
            Position = s.position,
            Weight = 1
        }).ToList();

        var scenario = new StageScenario
        {
            Id = Guid.NewGuid(),
            Weight = 1,
            Slots = scenarioSlots
        };

        var stage = new StageProfile
        {
            Id = Guid.NewGuid(),
            StageIndex = stageIndex,
            Weight = 1,
            Scenarios = new List<StageScenario> { scenario }
        };

        db.Set<StageProfile>().Add(stage);
        await db.SaveChangesAsync();

        return profileId;
    }

    [Fact]
    public async Task GenerateEnemies_ShouldReturnEnemies()
    {
        var (db, _) = Create();
        await SeedStageAsync(db, new List<(EnemyClass, int)>
        {
            (EnemyClass.Tank, 1)
        });

        var service = CreateService(db);
        var result = await service.GenerateEnemiesAsync(0);

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GenerateEnemies_ShouldThrow_WhenNoStage()
    {
        var (db, _) = Create();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateEnemiesAsync(99));
    }

    [Fact]
    public async Task GenerateEnemies_ShouldRespectMaxFourEnemies()
    {
        var (db, _) = Create();
        await SeedStageAsync(db, new List<(EnemyClass, int)>
        {
            (EnemyClass.Tank,      1),
            (EnemyClass.Polearm,   2),
            (EnemyClass.Crossbow,  2),
            (EnemyClass.Archer,    3),
            (EnemyClass.Archer,    4)
        });

        var service = CreateService(db);
        var result = await service.GenerateEnemiesAsync(0);

        Assert.True(result.Count <= 4);
    }

    [Fact]
    public async Task GenerateEnemies_ShouldSkipInvalidPositions()
    {
        var (db, _) = Create();
        await SeedStageAsync(db, new List<(EnemyClass, int)>
        {
            (EnemyClass.Tank, 3)
        });

        var service = CreateService(db);
        var result = await service.GenerateEnemiesAsync(0);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateEnemies_EnemyShouldHaveCorrectHp()
    {
        var (db, _) = Create();
        await SeedStageAsync(db, new List<(EnemyClass, int)>
        {
            (EnemyClass.Tank, 1)
        });

        var service = CreateService(db);
        var result = await service.GenerateEnemiesAsync(0);

        Assert.All(result, e =>
        {
            Assert.Equal(100, e.CurrentHp);
            Assert.Equal(100, e.MaxHp);
        });
    }

    [Fact]
    public async Task GenerateEnemies_EachEnemyShouldHaveUniqueId()
    {
        var (db, _) = Create();
        await SeedStageAsync(db, new List<(EnemyClass, int)>
        {
            (EnemyClass.Tank,    1),
            (EnemyClass.Polearm, 2)
        });

        var service = CreateService(db);
        var result = await service.GenerateEnemiesAsync(0);

        var ids = result.Select(e => e.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public async Task GenerateEnemies_ShouldCallApplyEnemyEquipment()
    {
        var (db, _) = Create();
        var mockEquipment = CreateDefaultEquipmentSvc();

        await SeedStageAsync(db, new List<(EnemyClass, int)>
        {
            (EnemyClass.Tank, 1)
        });

        var service = CreateService(db, equipmentSvc: mockEquipment);
        await service.GenerateEnemiesAsync(0);

        mockEquipment.Verify(
            x => x.ApplyEnemyEquipment(It.IsAny<Equipment>(), It.IsAny<List<Item>>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateEnemies_Archer_ShouldOnlySpawnAtPosition3Or4()
    {
        var (db, _) = Create();
        await SeedStageAsync(db, new List<(EnemyClass, int)>
        {
            (EnemyClass.Archer, 3)
        });

        var service = CreateService(db);
        var result = await service.GenerateEnemiesAsync(0);

        Assert.Single(result);
        Assert.Equal(3, result[0].Position);
    }

    [Fact]
    public async Task GenerateEnemies_ShouldThrow_WhenStageHasNoScenarios()
    {
        var (db, _) = Create();

        db.Set<StageProfile>().Add(new StageProfile
        {
            Id = Guid.NewGuid(),
            StageIndex = 0,
            Weight = 1,
            Scenarios = new List<StageScenario>()
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateEnemiesAsync(0));
    }

    [Fact]
    public async Task GenerateEnemies_ShouldAssignCorrectClass()
    {
        var (db, _) = Create();
        await SeedStageAsync(db, new List<(EnemyClass, int)>
        {
            (EnemyClass.Archer, 3)
        });

        var service = CreateService(db);
        var result = await service.GenerateEnemiesAsync(0);

        Assert.All(result, e => Assert.Equal(EnemyClass.Archer, e.Class));
    }

    [Fact]
    public async Task GenerateEnemies_Tank_ShouldOnlySpawnAtPosition1()
    {
        var (db, _) = Create();
        await SeedStageAsync(db, new List<(EnemyClass, int)>
        {
            (EnemyClass.Tank, 2)
        });

        var service = CreateService(db);
        var result = await service.GenerateEnemiesAsync(0);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateEnemies_MultipleStages_ShouldUseCorrectStage()
    {
        var (db, _) = Create();
        await SeedStageAsync(db, new List<(EnemyClass, int)> { (EnemyClass.Tank, 1) }, stageIndex: 0);
        await SeedStageAsync(db, new List<(EnemyClass, int)> { (EnemyClass.Archer, 3) }, stageIndex: 1);

        var service = CreateService(db);
        var result0 = await service.GenerateEnemiesAsync(0);
        var result1 = await service.GenerateEnemiesAsync(1);

        Assert.All(result0, e => Assert.Equal(EnemyClass.Tank, e.Class));
        Assert.All(result1, e => Assert.Equal(EnemyClass.Archer, e.Class));
    }
}