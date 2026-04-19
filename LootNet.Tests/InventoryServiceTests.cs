namespace LootNet_API.Tests;

using LootNet_API.Data;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Services;
using LootNet_API.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class InventoryServiceTests
{
    private AppDbContext CreateDb()
        => TestDbContextFactory.Create();

    private async Task<User> SeedUser(AppDbContext db)
    {
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Username = "player",
            PasswordHash = "hash",
            Role = UserRole.Player,
            Currency = 1000,
            Equipment = new Equipment
            {
                Id = Guid.NewGuid(),
                UserId = userId
            }
        };

        db.Users.Add(user);
        db.Equipments.Add(user.Equipment);

        await db.SaveChangesAsync();

        return user;
    }

    [Fact]
    public async Task MoveToRun_ShouldMoveItem()
    {
        var db = CreateDb();
        var user = await SeedUser(db);

        var itemId = Guid.NewGuid();

        db.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ItemId = itemId
        });

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await service.MoveToRunAsync(user.Id, new List<Guid> { itemId });

        Assert.Empty(db.InventoryItems);
        Assert.Single(db.RunInventoryItems);
    }

    [Fact]
    public async Task ReturnFromRun_ShouldRestoreInventory()
    {
        var db = CreateDb();
        var user = await SeedUser(db);

        var itemId = Guid.NewGuid();

        db.RunInventoryItems.Add(new RunInventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ItemId = itemId
        });

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await service.ReturnFromRunAsync(user.Id);

        Assert.Single(db.InventoryItems);
        Assert.Empty(db.RunInventoryItems);
    }

    [Fact]
    public async Task MoveToMarket_ShouldMoveItem()
    {
        var db = CreateDb();
        var user = await SeedUser(db);

        var itemId = Guid.NewGuid();

        db.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ItemId = itemId
        });

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await service.MoveToMarketAsync(user.Id, itemId);

        Assert.Empty(db.InventoryItems);
        Assert.Single(db.MarketInventoryItems);
    }

    [Fact]
    public async Task ReturnFromMarket_ShouldRestoreInventory()
    {
        var db = CreateDb();
        var user = await SeedUser(db);

        var itemId = Guid.NewGuid();

        db.MarketInventoryItems.Add(new MarketInventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ItemId = itemId
        });

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await service.ReturnFromMarketAsync(user.Id, itemId);

        Assert.Single(db.InventoryItems);
        Assert.Empty(db.MarketInventoryItems);
    }

    [Fact]
    public async Task TransferFromSellerToBuyer_ShouldMoveOwnership()
    {
        var db = CreateDb();

        var seller = await SeedUser(db);
        var buyer = await SeedUser(db);

        var itemId = Guid.NewGuid();

        db.MarketInventoryItems.Add(new MarketInventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = seller.Id,
            ItemId = itemId
        });

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await service.TransferFromSellerToBuyerAsync(seller.Id, buyer.Id, itemId);

        Assert.Empty(db.MarketInventoryItems);
        Assert.Single(db.InventoryItems);
    }

    [Fact]
    public async Task MoveToRun_ShouldThrow_WhenItemNotInInventory()
    {
        var db = CreateDb();
        var user = await SeedUser(db);

        var service = new InventoryService(db);

        await service.MoveToRunAsync(user.Id, new List<Guid> { Guid.NewGuid() });

        Assert.Empty(db.RunInventoryItems);
    }

    [Fact]
    public async Task EquipWeaponFromRun_ShouldEquip()
    {
        var db = CreateDb();
        var user = await SeedUser(db);

        var itemId = Guid.NewGuid();

        db.RunInventoryItems.Add(new RunInventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ItemId = itemId
        });

        db.Weapons.Add(new Weapon
        {
            Id = itemId,
            Name = "Sword",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword
        });

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await service.EquipWeaponFromRunAsync(user.Id, itemId, 1);

        var eq = await db.Equipments.FirstAsync(x => x.UserId == user.Id);

        Assert.Equal(itemId, eq.WeaponSlot1Id);
    }

    [Fact]
    public async Task EquipWeaponFromRun_ShouldThrow_WhenNotInRun()
    {
        var db = CreateDb();
        var user = await SeedUser(db);

        var service = new InventoryService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EquipWeaponFromRunAsync(user.Id, Guid.NewGuid(), 1));
    }

    [Fact]
    public async Task GetRunInventory_ShouldReturnItems()
    {
        var db = CreateDb();
        var user = await SeedUser(db);

        var itemId = Guid.NewGuid();

        db.RunInventoryItems.Add(new RunInventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ItemId = itemId
        });

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        var result = await service.GetRunInventoryAsync(user.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMarketInventory_ShouldReturnItems()
    {
        var db = CreateDb();
        var user = await SeedUser(db);

        var itemId = Guid.NewGuid();

        db.MarketInventoryItems.Add(new MarketInventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ItemId = itemId
        });

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        var result = await service.GetMarketInventoryAsync(user.Id);

        Assert.NotNull(result);
    }
}