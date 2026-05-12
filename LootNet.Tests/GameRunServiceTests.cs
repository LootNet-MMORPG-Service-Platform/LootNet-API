using LootNet_API.Data;
using LootNet_API.DTO.GameRun;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.GameRun;
using LootNet_API.Models.Items;
using LootNet_API.Services;
using LootNet_API.Services.Interfaces;
using LootNet_API.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LootNet_API.Tests;

public class GameRunServiceTests
{
    private (GameRunService service, AppDbContext db, Mock<IEnemyGenerationService> mock)
    CreateService(Func<Mock<IEnemyGenerationService>>? enemyGenFactory = null)
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = new TestDbContextFactory(dbName);
        var testDb = factory.CreateDbContext();

        var equipmentService = new EquipmentService(factory);
        var inventoryService = new InventoryService(factory);
        var combatHands = new CombatHandsService();
        var damageCalc = new DamageCalculator(equipmentService);
        var battleService = new BattleService(combatHands, damageCalc, equipmentService);

        var mock = enemyGenFactory != null ? enemyGenFactory() : DefaultEnemyMock();

        var service = new GameRunService(factory, battleService, mock.Object, inventoryService);
        return (service, testDb, mock);
    }

    private Mock<IEnemyGenerationService> DefaultEnemyMock()
    {
        var m = new Mock<IEnemyGenerationService>();
        m.Setup(x => x.GenerateEnemiesAsync(It.IsAny<int>()))
         .ReturnsAsync(() => new List<BattleEnemy>
         {
             new BattleEnemy
             {
                 Id        = Guid.NewGuid(),
                 Position  = 1,
                 CurrentHp = 100,
                 MaxHp     = 100,
                 Equipment = new Equipment { Id = Guid.NewGuid(), UserId = Guid.NewGuid() }
             }
         });
        return m;
    }


    private async Task<(Guid userId, Guid weaponId)> SeedUserAsync(AppDbContext db)
    {
        var userId = Guid.NewGuid();

        var sword = new Weapon
        {
            Id = Guid.NewGuid(),
            Name = "Sword",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Cut = 10,
            Blunt = 0,
            Elements = new List<ItemElement>()
        };
        db.Weapons.Add(sword);
        await db.SaveChangesAsync();

        var user = new User
        {
            Id = userId,
            Username = $"user_{userId}",
            PasswordHash = "hash",
            Equipment = new Equipment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WeaponSlot1Id = sword.Id
            },
            InventoryItems = new List<InventoryItem>
        {
            new InventoryItem { Id = Guid.NewGuid(), UserId = userId, ItemId = sword.Id }
        }
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        return (userId, sword.Id);
    }

    private async Task SetRunLeftHandAsync(AppDbContext db, Guid userId, Guid weaponId)
    {
        var run = await db.Runs.FirstAsync(x => x.UserId == userId);
        run.LeftHandItemId = weaponId;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private async Task SetEnemyHpAsync(AppDbContext db, Guid battleId, int hp)
    {
        var enemy = await db.BattleEnemies.FirstAsync(x => x.BattleId == battleId);
        enemy.CurrentHp = hp;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private async Task SetPlayerHpAsync(AppDbContext db, Guid userId, int hp)
    {
        var run = await db.Runs.FirstAsync(x => x.UserId == userId);
        run.PlayerCurrentHp = hp;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static BattleEnemy MakeEnemy(
        int position = 1,
        int hp = 100,
        Guid? leftWeaponId = null,
        Equipment? equipment = null)
        => new BattleEnemy
        {
            Id = Guid.NewGuid(),
            Position = position,
            CurrentHp = hp,
            MaxHp = 100,
            LeftHandItemId = leftWeaponId,
            Equipment = equipment ?? new Equipment { Id = Guid.NewGuid(), UserId = Guid.NewGuid() }
        };

    private static Battle MakeBattle(Guid runId, BattleEnemy enemy)
    {
        var id = Guid.NewGuid();
        enemy.BattleId = id;
        return new Battle { Id = id, RunId = runId, Enemies = new List<BattleEnemy> { enemy } };
    }


    [Fact]
    public async Task StartRun_ShouldCreateRun()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        var result = await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid> { weaponId } });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(RunStatus.Active, result.Status);
        Assert.Equal(0, result.BattleIndex);
        Assert.True(await db.Runs.AnyAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task StartRun_ShouldMoveItemsToRunInventory()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid> { weaponId } });

        Assert.False(await db.InventoryItems.AnyAsync(x => x.UserId == userId && x.ItemId == weaponId));
        Assert.True(await db.RunInventoryItems.AnyAsync(x => x.UserId == userId && x.ItemId == weaponId));
    }

    [Fact]
    public async Task StartRun_WithNoItems_ShouldCreateEmptyRunInventory()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        var result = await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid>() });

        Assert.Equal(RunStatus.Active, result.Status);
        Assert.False(await db.RunInventoryItems.AnyAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task StartRun_ShouldReturnExisting_WhenRunAlreadyActive()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.Active,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid>() });
        var runs = await db.Runs.AsNoTracking().Where(x => x.UserId == userId).ToListAsync();
        Assert.Single(runs);
        Assert.Equal(runs[0].Id, result.Id);
        Assert.Equal(RunStatus.Active, result.Status);
    }

    [Fact]
    public async Task StartRun_ShouldReturnExisting_WhenRunInBattle()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid>() });
        var runs = await db.Runs.AsNoTracking().Where(x => x.UserId == userId).ToListAsync();
        Assert.Single(runs);
        Assert.Equal(runs[0].Id, result.Id);
        Assert.Equal(RunStatus.InBattle, result.Status);
    }

    [Fact]
    public async Task StartRun_AllowsNewRun_AfterLost()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.Lost,
            PlayerCurrentHp = 0,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        db.InventoryItems.Add(new InventoryItem { Id = Guid.NewGuid(), UserId = userId, ItemId = weaponId });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid> { weaponId } });

        Assert.Equal(RunStatus.Active, result.Status);
    }


    [Fact]
    public async Task GoFurther_ShouldCreateBattleAndSetInBattle()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.Active,
            BattleIndex = 0,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.GoFurtherAsync(userId);

        Assert.NotEqual(Guid.Empty, result.BattleId);
        Assert.NotEmpty(result.Enemies);
        Assert.Equal(RunStatus.InBattle,
            (await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId)).Status);
    }

    [Fact]
    public async Task GoFurther_ShouldCallEnemyGen_WithCorrectBattleIndex()
    {
        var (service, db, mock) = CreateService();
        var (userId, _) = await SeedUserAsync(db);
        int capturedIndex = -1;

        mock.Setup(x => x.GenerateEnemiesAsync(It.IsAny<int>()))
            .Callback<int>(i => capturedIndex = i)
            .ReturnsAsync(new List<BattleEnemy>
            {
                new BattleEnemy
                {
                    Id = Guid.NewGuid(), Position = 1, CurrentHp = 100, MaxHp = 100,
                    Equipment = new Equipment { Id = Guid.NewGuid(), UserId = Guid.NewGuid() }
                }
            });

        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.Active,
            BattleIndex = 5,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await service.GoFurtherAsync(userId);

        Assert.Equal(5, capturedIndex);
    }

    [Fact]
    public async Task GoFurther_ShouldReturnEnemyDetails()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.Active,
            BattleIndex = 0,
            PlayerCurrentHp = 75,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var dto = await service.GoFurtherAsync(userId);

        Assert.Equal(75, dto.PlayerCurrentHp);
        Assert.Equal(100, dto.PlayerMaxHp);
        Assert.Single(dto.Enemies);
        Assert.Equal(1, dto.Enemies[0].Position);
        Assert.Equal(100, dto.Enemies[0].CurrentHp);
    }

    [Fact]
    public async Task GoFurther_ShouldThrow_WhenRunInBattle()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GoFurtherAsync(userId));
    }

    [Fact]
    public async Task GoFurther_ShouldThrow_WhenNoActiveRun()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GoFurtherAsync(userId));
    }


    [Fact]
    public async Task FinishTurn_SkipTurn_ShouldWork()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        var runId = Guid.NewGuid();
        var battle = MakeBattle(runId, MakeEnemy(position: 1));
        db.Runs.Add(new Run
        {
            Id = runId,
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            PlayerPosition = 0,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle> { battle }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.FinishTurnAsync(userId, new FinishTurnDTO
        {
            BattleId = battle.Id,
            Action = new TurnActionDTO { Type = ActionType.SkipTurn }
        });

        Assert.True(result.PlayerSkipped);
        Assert.False(result.RunFinished);
    }

    [Fact]
    public async Task FinishTurn_ChangePosition_ShouldUpdatePositionAndMarkDisorganized()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        var runId = Guid.NewGuid();
        var battle = MakeBattle(runId, MakeEnemy(position: 4));
        db.Runs.Add(new Run
        {
            Id = runId,
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            PlayerPosition = 0,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle> { battle }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await service.FinishTurnAsync(userId, new FinishTurnDTO
        {
            BattleId = battle.Id,
            Action = new TurnActionDTO { Type = ActionType.ChangePosition, TargetPosition = 2 }
        });

        var run = await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId);
        Assert.Equal(2, run.PlayerPosition);
        Assert.True(run.IsPlayerDisorganized);
    }

    [Fact]
    public async Task FinishTurn_ChangePosition_ShouldClampToMaxPosition()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        var runId = Guid.NewGuid();
        var battle = MakeBattle(runId, MakeEnemy(position: 4));
        db.Runs.Add(new Run
        {
            Id = runId,
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            PlayerPosition = 0,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle> { battle }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await service.FinishTurnAsync(userId, new FinishTurnDTO
        {
            BattleId = battle.Id,
            Action = new TurnActionDTO { Type = ActionType.ChangePosition, TargetPosition = 99 }
        });

        var run = await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId);
        Assert.Equal(3, run.PlayerPosition);
    }


    [Fact]
    public async Task FinishTurn_PlayerAttack_KillsEnemy_ShouldFinishBattle()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        var runId = Guid.NewGuid();
        var battle = MakeBattle(runId, MakeEnemy(position: 1, hp: 1));
        db.Runs.Add(new Run
        {
            Id = runId,
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            PlayerPosition = 0,
            LeftHandItemId = weaponId,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle> { battle }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.FinishTurnAsync(userId, new FinishTurnDTO
        {
            BattleId = battle.Id,
            Action = new TurnActionDTO { Type = ActionType.Attack, TargetPosition = 1 }
        });

        Assert.True(result.RunFinished);
        Assert.True(result.DamageDealt > 0);

        var run = await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId);
        Assert.Equal(RunStatus.Active, run.Status);
        Assert.Equal(1, run.BattleIndex);
    }

    [Fact]
    public async Task FinishTurn_OutOfMeleeRange_ShouldNotDealDamage()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        var runId = Guid.NewGuid();
        var enemy = MakeEnemy(position: 3);
        var battle = MakeBattle(runId, enemy);
        db.Runs.Add(new Run
        {
            Id = runId,
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            PlayerPosition = 0,
            LeftHandItemId = weaponId,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle> { battle }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.FinishTurnAsync(userId, new FinishTurnDTO
        {
            BattleId = battle.Id,
            Action = new TurnActionDTO { Type = ActionType.Attack, TargetPosition = 3 }
        });

        Assert.Equal(0, result.DamageDealt);
        var reloaded = await db.BattleEnemies.AsNoTracking().FirstAsync(x => x.Id == enemy.Id);
        Assert.Equal(100, reloaded.CurrentHp);
    }

    [Fact]
    public async Task FinishTurn_AttackNonExistentPosition_ShouldNotDealDamage()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        var runId = Guid.NewGuid();
        var battle = MakeBattle(runId, MakeEnemy(position: 1));
        db.Runs.Add(new Run
        {
            Id = runId,
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            PlayerPosition = 0,
            LeftHandItemId = weaponId,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle> { battle }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.FinishTurnAsync(userId, new FinishTurnDTO
        {
            BattleId = battle.Id,
            Action = new TurnActionDTO { Type = ActionType.Attack, TargetPosition = 4 }
        });

        Assert.Equal(0, result.DamageDealt);
    }

    [Fact]
    public async Task FinishTurn_PlayerDies_ShouldLoseRun()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        var enemyWeapon = new Weapon
        {
            Id = Guid.NewGuid(),
            Name = "EnemySword",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Cut = 9999,
            Blunt = 0,
            Elements = new List<ItemElement>()
        };
        db.Weapons.Add(enemyWeapon);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var runId = Guid.NewGuid();
        var enemy = MakeEnemy(
            position: 1, hp: 100,
            leftWeaponId: enemyWeapon.Id,
            equipment: new Equipment { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), WeaponSlot1Id = enemyWeapon.Id }
        );
        var battle = MakeBattle(runId, enemy);
        db.Runs.Add(new Run
        {
            Id = runId,
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 1,
            PlayerMaxHp = 100,
            PlayerPosition = 0,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle> { battle }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.FinishTurnAsync(userId, new FinishTurnDTO
        {
            BattleId = battle.Id,
            Action = new TurnActionDTO { Type = ActionType.SkipTurn }
        });

        Assert.True(result.RunFinished);

        var run = await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId);
        Assert.Equal(RunStatus.Lost, run.Status);
        Assert.False(await db.RunInventoryItems.AnyAsync(x => x.UserId == userId));
        Assert.True(await db.Weapons.AnyAsync(x => x.Id == weaponId));
        Assert.False(await db.RunInventoryItems.AnyAsync(x => x.UserId == userId));

        var eq = await db.Equipments.AsNoTracking().FirstAsync(x => x.UserId == userId);
        Assert.Null(eq.WeaponSlot1Id);
    }

    [Fact]
    public async Task FinishTurn_PlayerStunned_ShouldSkipPlayerAndLetEnemyAct()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        var enemyWeapon = new Weapon
        {
            Id = Guid.NewGuid(),
            Name = "EnemyBow",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Bow,
            Cut = 5,
            Blunt = 0,
            Elements = new List<ItemElement>()
        };
        db.Weapons.Add(enemyWeapon);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var runId = Guid.NewGuid();
        var enemy = MakeEnemy(
            position: 2, hp: 100,
            leftWeaponId: enemyWeapon.Id,
            equipment: new Equipment { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), WeaponSlot1Id = enemyWeapon.Id }
        );
        var battle = MakeBattle(runId, enemy);
        db.Runs.Add(new Run
        {
            Id = runId,
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            PlayerPosition = 0,
            PlayerSkipNextTurn = true,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle> { battle }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.FinishTurnAsync(userId, new FinishTurnDTO
        {
            BattleId = battle.Id,
            Action = new TurnActionDTO { Type = ActionType.SkipTurn }
        });

        Assert.True(result.EnemyDamage > 0);
        var run = await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId);
        Assert.False(run.PlayerSkipNextTurn);
    }


    [Fact]
    public async Task FinishTurn_ShouldThrow_WhenNotInBattle()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.Active,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FinishTurnAsync(userId, new FinishTurnDTO
            {
                BattleId = Guid.NewGuid(),
                Action = new TurnActionDTO { Type = ActionType.SkipTurn }
            }));
    }

    [Fact]
    public async Task FinishTurn_ShouldThrow_WhenBattleNotFound()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FinishTurnAsync(userId, new FinishTurnDTO
            {
                BattleId = Guid.NewGuid(),
                Action = new TurnActionDTO { Type = ActionType.SkipTurn }
            }));
    }


    [Fact]
    public async Task EndRun_ShouldReturnItemsAndSetReturned()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        db.RunInventoryItems.Add(new RunInventoryItem { Id = Guid.NewGuid(), UserId = userId, ItemId = weaponId });
        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.Active,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.EndRunAsync(userId);

        Assert.Equal(RunStatus.Returned, result.Status);
        Assert.True(await db.InventoryItems.AnyAsync(x => x.UserId == userId && x.ItemId == weaponId));
        Assert.False(await db.RunInventoryItems.AnyAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task EndRun_ShouldThrow_WhenInBattle()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EndRunAsync(userId));
    }

    [Fact]
    public async Task EndRun_ShouldThrow_WhenNoRun()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EndRunAsync(userId));
    }


    [Fact]
    public async Task ForceReturn_ShouldReturnItemsAndSetForcedReturn()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        db.RunInventoryItems.Add(new RunInventoryItem { Id = Guid.NewGuid(), UserId = userId, ItemId = weaponId });
        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.InBattle,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.ForceReturnAsync(userId);

        Assert.Equal(RunStatus.ForcedReturn, result.Status);
        Assert.True(await db.InventoryItems.AnyAsync(x => x.UserId == userId && x.ItemId == weaponId));
        Assert.False(await db.RunInventoryItems.AnyAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task ForceReturn_ShouldWork_WhenRunIsActive()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        db.RunInventoryItems.Add(new RunInventoryItem { Id = Guid.NewGuid(), UserId = userId, ItemId = weaponId });
        db.Runs.Add(new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.Active,
            PlayerCurrentHp = 100,
            PlayerMaxHp = 100,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.ForceReturnAsync(userId);

        Assert.Equal(RunStatus.ForcedReturn, result.Status);
        Assert.True(await db.InventoryItems.AnyAsync(x => x.UserId == userId && x.ItemId == weaponId));
    }

    [Fact]
    public async Task ForceReturn_ShouldThrow_WhenNoActiveRun()
    {
        var (service, db, _) = CreateService();
        var (userId, _) = await SeedUserAsync(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ForceReturnAsync(userId));
    }

    [Fact]
    public async Task FullFlow_StartRun_FightEnemy_Win_EndRun()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        var runDto = await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid> { weaponId } });
        Assert.Equal(RunStatus.Active, runDto.Status);
        Assert.Equal(0, runDto.BattleIndex);
        Assert.False(await db.InventoryItems.AnyAsync(x => x.UserId == userId && x.ItemId == weaponId));
        Assert.True(await db.RunInventoryItems.AnyAsync(x => x.UserId == userId && x.ItemId == weaponId));

        var battleDto = await service.GoFurtherAsync(userId);
        Assert.NotEqual(Guid.Empty, battleDto.BattleId);
        Assert.Single(battleDto.Enemies);
        Assert.Equal(RunStatus.InBattle,
            (await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId)).Status);

        await SetEnemyHpAsync(db, battleDto.BattleId, 1);
        await SetRunLeftHandAsync(db, userId, weaponId);

        var turnResult = await service.FinishTurnAsync(userId, new FinishTurnDTO
        {
            BattleId = battleDto.BattleId,
            Action = new TurnActionDTO { Type = ActionType.Attack, TargetPosition = 1 }
        });

        Assert.True(turnResult.DamageDealt > 0);
        Assert.True(turnResult.RunFinished);

        var runAfterBattle = await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId);
        Assert.Equal(RunStatus.Active, runAfterBattle.Status);
        Assert.Equal(1, runAfterBattle.BattleIndex);
        var endDto = await service.EndRunAsync(userId);
        Assert.Equal(RunStatus.Returned, endDto.Status);
        Assert.True(await db.InventoryItems.AnyAsync(x => x.UserId == userId && x.ItemId == weaponId));
        Assert.False(await db.RunInventoryItems.AnyAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task FullFlow_StartRun_GetKilled_LoseEverything()
    {
        var enemyWeapon = new Weapon
        {
            Id = Guid.NewGuid(),
            Name = "Executioner",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Cut = 9999,
            Blunt = 0,
            Elements = new List<ItemElement>()
        };

        var strongMock = new Mock<IEnemyGenerationService>();
        strongMock.Setup(x => x.GenerateEnemiesAsync(It.IsAny<int>()))
            .ReturnsAsync(() => new List<BattleEnemy>
            {
                new BattleEnemy
                {
                    Id             = Guid.NewGuid(),
                    Position       = 1,
                    CurrentHp      = 100,
                    MaxHp          = 100,
                    LeftHandItemId = enemyWeapon.Id,
                    Equipment      = new Equipment
                    {
                        Id = Guid.NewGuid(), UserId = Guid.NewGuid(), WeaponSlot1Id = enemyWeapon.Id
                    }
                }
            });

        var (service, db, _) = CreateService(() => strongMock);
        var (userId, weaponId) = await SeedUserAsync(db);

        db.Weapons.Add(enemyWeapon);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid> { weaponId } });

        var battleDto = await service.GoFurtherAsync(userId);

        await SetPlayerHpAsync(db, userId, 1);

        var result = await service.FinishTurnAsync(userId, new FinishTurnDTO
        {
            BattleId = battleDto.BattleId,
            Action = new TurnActionDTO { Type = ActionType.SkipTurn }
        });

        Assert.True(result.RunFinished);

        var lostRun = await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId);
        Assert.Equal(RunStatus.Lost, lostRun.Status);

        Assert.False(await db.RunInventoryItems.AnyAsync(x => x.UserId == userId));
        Assert.True(await db.Weapons.AnyAsync(x => x.Id == weaponId));
        Assert.False(await db.RunInventoryItems.AnyAsync(x => x.UserId == userId));

        var eq = await db.Equipments.AsNoTracking().FirstAsync(x => x.UserId == userId);
        Assert.Null(eq.WeaponSlot1Id);
        Assert.Null(eq.WeaponSlot2Id);
        Assert.Null(eq.WeaponSlot3Id);
        Assert.Null(eq.WeaponSlot4Id);
    }

    [Fact]
    public async Task FullFlow_MultiBattle_TwoBattlesWon_ThenReturn()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid> { weaponId } });

        async Task WinBattle(int expectedIndex)
        {
            await SetRunLeftHandAsync(db, userId, weaponId);

            var before = await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId);
            Assert.Equal(RunStatus.Active, before.Status);
            Assert.Equal(expectedIndex, before.BattleIndex);

            var battleDto = await service.GoFurtherAsync(userId);
            Assert.Equal(RunStatus.InBattle,
                (await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId)).Status);

            await SetEnemyHpAsync(db, battleDto.BattleId, 1);

            var result = await service.FinishTurnAsync(userId, new FinishTurnDTO
            {
                BattleId = battleDto.BattleId,
                Action = new TurnActionDTO { Type = ActionType.Attack, TargetPosition = 1 }
            });

            Assert.True(result.RunFinished);
            Assert.True(result.DamageDealt > 0);
        }

        await WinBattle(expectedIndex: 0);
        var after1 = await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId);
        Assert.Equal(RunStatus.Active, after1.Status);
        Assert.Equal(1, after1.BattleIndex);

        await WinBattle(expectedIndex: 1);
        var after2 = await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId);
        Assert.Equal(RunStatus.Active, after2.Status);
        Assert.Equal(2, after2.BattleIndex);

        var endDto = await service.EndRunAsync(userId);
        Assert.Equal(RunStatus.Returned, endDto.Status);
        Assert.True(await db.InventoryItems.AnyAsync(x => x.UserId == userId && x.ItemId == weaponId));
    }

    [Fact]
    public async Task FullFlow_StartRun_GoFurther_ForceReturn()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid> { weaponId } });
        Assert.True(await db.RunInventoryItems.AnyAsync(x => x.UserId == userId));

        await service.GoFurtherAsync(userId);
        Assert.Equal(RunStatus.InBattle,
            (await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId)).Status);

        var dto = await service.ForceReturnAsync(userId);

        Assert.Equal(RunStatus.ForcedReturn, dto.Status);
        Assert.True(await db.InventoryItems.AnyAsync(x => x.UserId == userId && x.ItemId == weaponId));
        Assert.False(await db.RunInventoryItems.AnyAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task FullFlow_SecondStartReturnsSameRunWhileActive()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid> { weaponId } });

        db.InventoryItems.Add(new InventoryItem { Id = Guid.NewGuid(), UserId = userId, ItemId = weaponId });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var firstRun = await db.Runs.AsNoTracking().FirstAsync(x => x.UserId == userId);
        var secondStart = await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid> { weaponId } });
        var runs = await db.Runs.AsNoTracking().Where(x => x.UserId == userId).ToListAsync();
        Assert.Single(runs);
        Assert.Equal(firstRun.Id, secondStart.Id);
    }

    [Fact]
    public async Task FullFlow_CannotFinishTurnWithoutBattle()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid> { weaponId } });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FinishTurnAsync(userId, new FinishTurnDTO
            {
                BattleId = Guid.NewGuid(),
                Action = new TurnActionDTO { Type = ActionType.SkipTurn }
            }));
    }

    [Fact]
    public async Task FullFlow_CannotGoFurtherTwice_WhileInBattle()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid> { weaponId } });
        await service.GoFurtherAsync(userId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GoFurtherAsync(userId));
    }

    [Fact]
    public async Task FullFlow_CannotEndRun_WhileInBattle()
    {
        var (service, db, _) = CreateService();
        var (userId, weaponId) = await SeedUserAsync(db);

        await service.StartRunAsync(userId, new StartRunDTO { ItemIds = new List<Guid> { weaponId } });
        await service.GoFurtherAsync(userId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EndRunAsync(userId));
    }
    public class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(string dbName)
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
        }

        public AppDbContext CreateDbContext()
        {
            return new AppDbContext(_options);
        }
    }
}
