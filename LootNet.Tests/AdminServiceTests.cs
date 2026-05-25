namespace LootNet_API.Tests;

using System;
using System.Linq;
using System.Threading.Tasks;
using LootNet_API.Data;
using LootNet_API.DTO.Admin;
using LootNet_API.DTO.Items;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.GameRun;
using LootNet_API.Models.Items;
using LootNet_API.Services;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class AdminServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private IInventoryService CreateInventoryStub()
    {
        return new FakeInventoryService();
    }

    private IEquipmentService CreateEquipmentStub()
    {
        return new FakeEquipmentService();
    }

    [Fact]
    public async Task GetUsers_ReturnsPagedSortedFilteredData()
    {
        using var db = CreateDb();

        db.Users.AddRange(
            new User { Id = Guid.NewGuid(), Username = "alpha", PasswordHash = "h", Role = UserRole.Player, Currency = 100, Equipment = new Equipment() },
            new User { Id = Guid.NewGuid(), Username = "beta", PasswordHash = "h", Role = UserRole.Admin, Currency = 300, Equipment = new Equipment(), IsBlocked = true },
            new User { Id = Guid.NewGuid(), Username = "charlie", PasswordHash = "h", Role = UserRole.Player, Currency = 200, Equipment = new Equipment() }
        );

        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetUsersAsync(new GetUsersQueryDTO
        {
            Page = 1,
            PageSize = 10,
            SortBy = "currency",
            SortDir = "desc"
        });

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(300, result.Items.First().Currency);
    }

    [Fact]
    public async Task GetUsers_FilterByRole_Works()
    {
        using var db = CreateDb();

        db.Users.AddRange(
            new User { Id = Guid.NewGuid(), Username = "a", PasswordHash = "h", Role = UserRole.Admin, Currency = 100, Equipment = new Equipment() },
            new User { Id = Guid.NewGuid(), Username = "b", PasswordHash = "h", Role = UserRole.Player, Currency = 100, Equipment = new Equipment() }
        );

        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetUsersAsync(new GetUsersQueryDTO
        {
            Page = 1,
            PageSize = 10,
            Role = UserRole.Admin
        });

        Assert.Single(result.Items);
        Assert.Equal(UserRole.Admin, result.Items.First().Role);
    }

    [Fact]
    public async Task GetUsers_SearchByUsername_Works()
    {
        using var db = CreateDb();

        db.Users.Add(new User { Id = Guid.NewGuid(), Username = "dragonSlayer", PasswordHash = "h", Role = UserRole.Player, Currency = 100, Equipment = new Equipment() });
        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetUsersAsync(new GetUsersQueryDTO
        {
            Search = "dragon"
        });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetUsers_Pagination_WorksCorrectly()
    {
        using var db = CreateDb();

        for (int i = 0; i < 30; i++)
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = $"user{i}",
                PasswordHash = "h",
                Role = UserRole.Player,
                Currency = i,
                Equipment = new Equipment()
            });
        }

        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetUsersAsync(new GetUsersQueryDTO
        {
            Page = 2,
            PageSize = 10,
            SortBy = "username"
        });

        Assert.Equal(30, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    public async Task GetAdminUsers_ReturnsOnlyAdministrativeRoles()
    {
        using var db = CreateDb();

        db.Users.AddRange(
            new User { Id = Guid.NewGuid(), Username = "player", PasswordHash = "h", Role = UserRole.Player, Currency = 10, Equipment = new Equipment() },
            new User { Id = Guid.NewGuid(), Username = "admin", PasswordHash = "h", Role = UserRole.Admin, Currency = 20, Equipment = new Equipment() },
            new User { Id = Guid.NewGuid(), Username = "super", PasswordHash = "h", Role = UserRole.SuperAdmin, Currency = 30, Equipment = new Equipment() },
            new User { Id = Guid.NewGuid(), Username = "moderator", PasswordHash = "h", Role = UserRole.GameModerator, Currency = 40, Equipment = new Equipment() }
        );
        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetAdminUsersAsync();

        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, x => x.Role == UserRole.Player);
        Assert.Contains(result, x => x.Role == UserRole.Admin);
        Assert.Contains(result, x => x.Role == UserRole.SuperAdmin);
        Assert.Contains(result, x => x.Role == UserRole.GameModerator);
    }

    [Fact]
    public async Task GetAdminUsers_MapsAdminUserListFields()
    {
        using var db = CreateDb();
        var adminId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = adminId,
            Username = "admin",
            PasswordHash = "h",
            Role = UserRole.Admin,
            Currency = 999,
            Equipment = new Equipment(),
            IsBlocked = true
        });
        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetAdminUsersAsync();

        var item = Assert.Single(result);
        Assert.Equal(adminId, item.Id);
        Assert.Equal("admin", item.Username);
        Assert.Equal(UserRole.Admin, item.Role);
        Assert.Equal(999, item.Currency);
        Assert.True(item.IsBlocked);
    }

    [Fact]
    public async Task GetAdminUsers_SortsByUsernameAscending()
    {
        using var db = CreateDb();

        db.Users.AddRange(
            new User { Id = Guid.NewGuid(), Username = "zeta", PasswordHash = "h", Role = UserRole.Admin, Equipment = new Equipment() },
            new User { Id = Guid.NewGuid(), Username = "alpha", PasswordHash = "h", Role = UserRole.SuperAdmin, Equipment = new Equipment() },
            new User { Id = Guid.NewGuid(), Username = "gamma", PasswordHash = "h", Role = UserRole.GameModerator, Equipment = new Equipment() }
        );
        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetAdminUsersAsync();

        Assert.Equal(new[] { "alpha", "gamma", "zeta" }, result.Select(x => x.Username));
    }

    [Fact]
    public async Task GetAdminUsers_ReturnsEmpty_WhenNoAdministrativeUsersExist()
    {
        using var db = CreateDb();

        db.Users.Add(new User { Id = Guid.NewGuid(), Username = "player", PasswordHash = "h", Role = UserRole.Player, Equipment = new Equipment() });
        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetAdminUsersAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUser_ReturnsUserDetails()
    {
        using var db = CreateDb();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "test",
            PasswordHash = "h",
            Role = UserRole.Player,
            Currency = 500,
            Equipment = new Equipment(),
            IsBlocked = true,
            BlockReason = "cheat",
            BlockedUntil = DateTime.UtcNow.AddDays(2)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetUserAsync(user.Id);

        Assert.Equal("test", result.Username);
        Assert.True(result.IsBlocked);
        Assert.Equal("cheat", result.BlockReason);
    }

    [Fact]
    public async Task BlockUser_SetsBlockedState_AndLogs()
    {
        using var db = CreateDb();

        var adminId = Guid.NewGuid();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "target",
            PasswordHash = "h",
            Role = UserRole.Player,
            Currency = 100,
            Equipment = new Equipment()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        await service.BlockUserAsync(adminId, user.Id, "spam", 3);

        var updated = await db.Users.FirstAsync(x => x.Id == user.Id);

        Assert.True(updated.IsBlocked);
        Assert.NotNull(updated.BlockReason);
        Assert.Single(db.AdminLogs.ToList());
    }

    [Fact]
    public async Task UnblockUser_ClearsBlockState()
    {
        using var db = CreateDb();

        var adminId = Guid.NewGuid();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "target",
            PasswordHash = "h",
            Role = UserRole.Player,
            Currency = 100,
            Equipment = new Equipment(),
            IsBlocked = true,
            BlockReason = "x",
            BlockedUntil = DateTime.UtcNow.AddDays(1)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        await service.UnblockUserAsync(adminId, user.Id);

        var updated = await db.Users.FirstAsync(x => x.Id == user.Id);

        Assert.False(updated.IsBlocked);
        Assert.Null(updated.BlockReason);
        Assert.Null(updated.BlockedUntil);
    }

    [Fact]
    public async Task ChangeRole_UpdatesRole_AndLogs()
    {
        using var db = CreateDb();

        var adminId = Guid.NewGuid();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "target",
            PasswordHash = "h",
            Role = UserRole.Player,
            Currency = 100,
            Equipment = new Equipment()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        await service.ChangeRoleAsync(adminId, user.Id, UserRole.Admin);

        var updated = await db.Users.FirstAsync(x => x.Id == user.Id);

        Assert.Equal(UserRole.Admin, updated.Role);
        Assert.Single(db.AdminLogs.ToList());
    }

    [Fact]
    public async Task GetUserInventory_ReturnsEmptyStub()
    {
        using var db = CreateDb();
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetUserInventoryAsync(Guid.NewGuid());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUserRunInventory_ReturnsEmptyStub()
    {
        using var db = CreateDb();
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetUserRunInventoryAsync(Guid.NewGuid());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUserMarketInventory_ReturnsEmptyStub()
    {
        using var db = CreateDb();
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetUserMarketInventoryAsync(Guid.NewGuid());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUserEquipment_ReturnsStub()
    {
        using var db = CreateDb();
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetUserEquipmentAsync(Guid.NewGuid());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetRunsAsync_ReturnsPagedRuns_WithDefaultOrdering()
    {
        using var db = CreateDb();
        var (user1, user2) = await SeedUsersForRuns(db);
        await SeedRunsForListing(db, user1.Id, user2.Id);

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetRunsAsync(new GetRunsQueryDTO
        {
            Page = 1,
            PageSize = 2
        });

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items[0].StartedAt >= result.Items[1].StartedAt);
    }

    [Fact]
    public async Task GetRunsAsync_FiltersByUserId()
    {
        using var db = CreateDb();
        var (user1, user2) = await SeedUsersForRuns(db);
        await SeedRunsForListing(db, user1.Id, user2.Id);
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetRunsAsync(new GetRunsQueryDTO
        {
            UserId = user1.Id,
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, x => Assert.Equal(user1.Id, x.UserId));
    }

    [Fact]
    public async Task GetRunsAsync_FiltersByStatus()
    {
        using var db = CreateDb();
        var (user1, user2) = await SeedUsersForRuns(db);
        await SeedRunsForListing(db, user1.Id, user2.Id);
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetRunsAsync(new GetRunsQueryDTO
        {
            Status = RunStatus.Active,
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(RunStatus.Active, result.Items[0].Status);
    }

    [Fact]
    public async Task GetRunsAsync_ReturnsEmpty_WhenNoRuns()
    {
        using var db = CreateDb();
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetRunsAsync(new GetRunsQueryDTO
        {
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetUserRunsAsync_ReturnsOnlyUserRuns()
    {
        using var db = CreateDb();
        var (user1, user2) = await SeedUsersForRuns(db);
        await SeedRunsForListing(db, user1.Id, user2.Id);
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetUserRunsAsync(user2.Id);

        Assert.Single(result);
        Assert.All(result, x => Assert.Equal(user2.Id, x.UserId));
    }

    [Fact]
    public async Task GetUserRunsAsync_ReturnsEmpty_WhenUserHasNoRuns()
    {
        using var db = CreateDb();
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var result = await service.GetUserRunsAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRunAsync_ReturnsDetailsWithBattlesAndEnemies()
    {
        using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "runner",
            PasswordHash = "h",
            Role = UserRole.Player,
            Equipment = new Equipment()
        };

        var run = new Run
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Status = RunStatus.InBattle,
            BattleIndex = 4,
            PlayerCurrentHp = 70,
            PlayerMaxHp = 100,
            PlayerPosition = 2,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        };

        var battle = new Battle
        {
            Id = Guid.NewGuid(),
            RunId = run.Id,
            Enemies = new List<BattleEnemy>
            {
                new BattleEnemy
                {
                    Id = Guid.NewGuid(),
                    BattleId = Guid.NewGuid(),
                    Class = EnemyClass.Tank,
                    Position = 1,
                    CurrentHp = 90,
                    MaxHp = 100,
                    Equipment = new Equipment()
                }
            }
        };

        run.Battles.Add(battle);

        db.Users.Add(user);
        db.Runs.Add(run);
        await db.SaveChangesAsync();

        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        var details = await service.GetRunAsync(run.Id);

        Assert.Equal(run.Id, details.Id);
        Assert.Equal(user.Id, details.UserId);
        Assert.Equal(RunStatus.InBattle, details.Status);
        Assert.Single(details.Battles);
        Assert.Single(details.Battles[0].Enemies);
        Assert.Equal(EnemyClass.Tank, details.Battles[0].Enemies[0].Class);
    }

    [Fact]
    public async Task GetRunAsync_Throws_WhenRunDoesNotExist()
    {
        using var db = CreateDb();
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetRunAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task BlockUser_EmitsRealtimeEvent()
    {
        using var db = CreateDb();
        var notifier = new CapturingRealtimeNotifier();
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub(), notifier);
        var user = new User { Id = Guid.NewGuid(), Username = "u", PasswordHash = "h", Role = UserRole.Player, Equipment = new Equipment() };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await service.BlockUserAsync(Guid.NewGuid(), user.Id, "spam", 2);

        Assert.Contains(notifier.Events, x => x.domain == "admin.user" && x.action == "blocked" && x.userId == user.Id);
    }

    [Fact]
    public async Task UnblockUser_EmitsRealtimeEvent()
    {
        using var db = CreateDb();
        var notifier = new CapturingRealtimeNotifier();
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub(), notifier);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "u",
            PasswordHash = "h",
            Role = UserRole.Player,
            Equipment = new Equipment(),
            IsBlocked = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await service.UnblockUserAsync(Guid.NewGuid(), user.Id);

        Assert.Contains(notifier.Events, x => x.domain == "admin.user" && x.action == "unblocked" && x.userId == user.Id);
    }

    [Fact]
    public async Task ChangeRole_EmitsRealtimeEvent()
    {
        using var db = CreateDb();
        var notifier = new CapturingRealtimeNotifier();
        var service = new AdminService(db, CreateInventoryStub(), CreateEquipmentStub(), notifier);
        var user = new User { Id = Guid.NewGuid(), Username = "u", PasswordHash = "h", Role = UserRole.Player, Equipment = new Equipment() };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await service.ChangeRoleAsync(Guid.NewGuid(), user.Id, UserRole.Admin);

        Assert.Contains(notifier.Events, x => x.domain == "admin.user" && x.action == "role-changed" && x.userId == user.Id);
    }

    private async Task<(User user1, User user2)> SeedUsersForRuns(AppDbContext db)
    {
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Username = "u1",
            PasswordHash = "h",
            Role = UserRole.Player,
            Equipment = new Equipment()
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Username = "u2",
            PasswordHash = "h",
            Role = UserRole.Player,
            Equipment = new Equipment()
        };

        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();
        return (user1, user2);
    }

    private async Task SeedRunsForListing(AppDbContext db, Guid user1Id, Guid user2Id)
    {
        db.Runs.AddRange(
            new Run
            {
                Id = Guid.NewGuid(),
                UserId = user1Id,
                Status = RunStatus.Active,
                BattleIndex = 1,
                PlayerCurrentHp = 100,
                PlayerMaxHp = 100,
                StartedAt = DateTime.UtcNow.AddMinutes(-10),
                Battles = new List<Battle>()
            },
            new Run
            {
                Id = Guid.NewGuid(),
                UserId = user1Id,
                Status = RunStatus.Returned,
                BattleIndex = 3,
                PlayerCurrentHp = 40,
                PlayerMaxHp = 100,
                StartedAt = DateTime.UtcNow.AddMinutes(-30),
                FinishedAt = DateTime.UtcNow.AddMinutes(-5),
                Battles = new List<Battle>()
            },
            new Run
            {
                Id = Guid.NewGuid(),
                UserId = user2Id,
                Status = RunStatus.Lost,
                BattleIndex = 2,
                PlayerCurrentHp = 0,
                PlayerMaxHp = 100,
                StartedAt = DateTime.UtcNow.AddMinutes(-20),
                FinishedAt = DateTime.UtcNow.AddMinutes(-15),
                Battles = new List<Battle>()
            });

        await db.SaveChangesAsync();
    }

    private class FakeInventoryService : IInventoryService
    {
        public Task<ItemCollectionDTO> GetItemsAsync(Guid userId) => Task.FromResult(new ItemCollectionDTO());
        public Task<ItemCollectionDTO> GetInventoryAsync(Guid userId) => Task.FromResult(new ItemCollectionDTO());
        public Task<ItemCollectionDTO> GetRunInventoryAsync(Guid userId) => Task.FromResult(new ItemCollectionDTO());
        public Task<ItemCollectionDTO> GetMarketInventoryAsync(Guid userId) => Task.FromResult(new ItemCollectionDTO());
        public Task<EquipmentResponseDTO> GetEquipmentAsync(Guid userId) => Task.FromResult(new EquipmentResponseDTO());
        public Task EquipWeaponAsync(Guid userId, Guid itemId, int slot) => throw new NotSupportedException();
        public Task EquipWeaponFromRunAsync(Guid userId, Guid itemId, int slot) => throw new NotSupportedException();
        public Task EquipArmorAsync(Guid userId, Guid itemId) => throw new NotSupportedException();
        public Task EquipArmorFromRunAsync(Guid userId, Guid itemId) => throw new NotSupportedException();
        public Task UnequipItemAsync(Guid userId, Guid itemId) => throw new NotSupportedException();
        public Task MoveToRunAsync(Guid userId, List<Guid> itemIds) => throw new NotSupportedException();
        public Task ReturnFromRunAsync(Guid userId) => throw new NotSupportedException();
        public Task MoveToMarketAsync(Guid userId, Guid itemId) => throw new NotSupportedException();
        public Task ReturnFromMarketAsync(Guid userId, Guid itemId) => throw new NotSupportedException();
        public Task AddToInventoryAsync(Guid userId, Guid itemId) => throw new NotSupportedException();
        public Task AddToRunInventoryAsync(Guid userId, Guid itemId) => throw new NotSupportedException();
        public Task TransferFromSellerToBuyerAsync(Guid sellerId, Guid buyerId, Guid itemId) => throw new NotSupportedException();
        public Task LoseRunItemsAsync(Guid Id) => throw new NotSupportedException();
    }

    private class FakeEquipmentService : IEquipmentService
    {
        public Task<EquipmentResponseDTO> GetEquipmentAsync(Guid userId)
            => Task.FromResult(new EquipmentResponseDTO());

        public Task<WeaponDTO?> GetWeapon(Guid? id)
            => Task.FromResult<WeaponDTO?>(null);

        public Task<ArmorDTO?> GetArmor(Guid? id)
            => Task.FromResult<ArmorDTO?>(null);

        public Task EquipWeaponAsync(Guid userId, Guid itemId, int slot)
            => throw new NotSupportedException();

        public Task EquipWeaponFromRunAsync(Guid userId, Guid itemId, int slot)
            => throw new NotSupportedException();

        public Task EquipArmorAsync(Guid userId, Guid itemId)
            => throw new NotSupportedException();

        public Task EquipArmorFromRunAsync(Guid userId, Guid itemId)
            => throw new NotSupportedException();

        public Task UnequipItemAsync(Guid userId, Guid itemId)
            => throw new NotSupportedException();
        public Task<Weapon?> GetWeaponModelAsync(Guid id) => throw new NotSupportedException();
        public Task<Equipment?> GetEquipmentModelAsync(Guid id) => throw new NotSupportedException();
        public void ApplyEnemyEquipment(Equipment equipment, List<Item> list) => throw new NotSupportedException();
    }

    private class CapturingRealtimeNotifier : IRealtimeNotifier
    {
        public List<(string domain, string action, Guid? userId, object? data)> Events { get; } = new();

        public Task AppChangedAsync(string domain, string action, Guid? userId = null, object? data = null)
        {
            Events.Add((domain, action, userId, data));
            return Task.CompletedTask;
        }
    }
}
