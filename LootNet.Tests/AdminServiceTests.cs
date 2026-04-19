namespace LootNet_API.Tests;

using System;
using System.Linq;
using System.Threading.Tasks;
using LootNet_API.Data;
using LootNet_API.DTO.Admin;
using LootNet_API.DTO.Items;
using LootNet_API.Enums;
using LootNet_API.Models;
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

        var service = new AdminService(db, CreateInventoryStub());

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

        var service = new AdminService(db, CreateInventoryStub());

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

        var service = new AdminService(db, CreateInventoryStub());

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

        var service = new AdminService(db, CreateInventoryStub());

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

        var service = new AdminService(db, CreateInventoryStub());

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

        var service = new AdminService(db, CreateInventoryStub());

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

        var service = new AdminService(db, CreateInventoryStub());

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

        var service = new AdminService(db, CreateInventoryStub());

        await service.ChangeRoleAsync(adminId, user.Id, UserRole.Admin);

        var updated = await db.Users.FirstAsync(x => x.Id == user.Id);

        Assert.Equal(UserRole.Admin, updated.Role);
        Assert.Single(db.AdminLogs.ToList());
    }

    [Fact]
    public async Task GetUserInventory_ReturnsEmptyStub()
    {
        using var db = CreateDb();
        var service = new AdminService(db, CreateInventoryStub());

        var result = await service.GetUserInventoryAsync(Guid.NewGuid());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUserRunInventory_ReturnsEmptyStub()
    {
        using var db = CreateDb();
        var service = new AdminService(db, CreateInventoryStub());

        var result = await service.GetUserRunInventoryAsync(Guid.NewGuid());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUserMarketInventory_ReturnsEmptyStub()
    {
        using var db = CreateDb();
        var service = new AdminService(db, CreateInventoryStub());

        var result = await service.GetUserMarketInventoryAsync(Guid.NewGuid());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUserEquipment_ReturnsStub()
    {
        using var db = CreateDb();
        var service = new AdminService(db, CreateInventoryStub());

        var result = await service.GetUserEquipmentAsync(Guid.NewGuid());

        Assert.NotNull(result);
    }

    private class FakeInventoryService : IInventoryService
    {
        public Task<ItemCollectionDTO> GetItemsAsync(Guid userId)
            => Task.FromResult(new ItemCollectionDTO());

        public Task<ItemCollectionDTO> GetInventoryAsync(Guid userId)
            => Task.FromResult(new ItemCollectionDTO());

        public Task<ItemCollectionDTO> GetRunInventoryAsync(Guid userId)
            => Task.FromResult(new ItemCollectionDTO());

        public Task<ItemCollectionDTO> GetMarketInventoryAsync(Guid userId)
            => Task.FromResult(new ItemCollectionDTO());

        public Task<EquipmentResponseDTO> GetEquipmentAsync(Guid userId)
            => Task.FromResult(new EquipmentResponseDTO());

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

        public Task MoveToRunAsync(Guid userId, List<Guid> itemIds)
            => throw new NotSupportedException();

        public Task ReturnFromRunAsync(Guid userId)
            => throw new NotSupportedException();

        public Task MoveToMarketAsync(Guid userId, Guid itemId)
            => throw new NotSupportedException();

        public Task ReturnFromMarketAsync(Guid userId, Guid itemId)
            => throw new NotSupportedException();

        public Task AddToInventoryAsync(Guid userId, Guid itemId)
            => throw new NotSupportedException();

        public Task AddToRunInventoryAsync(Guid userId, Guid itemId)
            => throw new NotSupportedException();

        public Task TransferFromSellerToBuyerAsync(Guid sellerId, Guid buyerId, Guid itemId)
            => throw new NotSupportedException();
    }
}