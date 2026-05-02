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
    private (AppDbContext db, SingleContextFactory factory) Create()
        => DbHelper.Create();

    private async Task<User> SeedUser(AppDbContext db)
    {
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Username = $"player_{userId}",
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
        await db.SaveChangesAsync();

        return user;
    }

    [Fact]
    public async Task MoveToRun_ShouldMoveItem()
    {
        var (db, factory) = Create();
        var user = await SeedUser(db);
        var itemId = Guid.NewGuid();

        db.InventoryItems.Add(new InventoryItem { Id = Guid.NewGuid(), UserId = user.Id, ItemId = itemId });
        await db.SaveChangesAsync();

        var service = new InventoryService(factory);
        await service.MoveToRunAsync(user.Id, new List<Guid> { itemId });

        Assert.Empty(db.InventoryItems);
        Assert.Single(db.RunInventoryItems);
    }

    [Fact]
    public async Task ReturnFromRun_ShouldRestoreInventory()
    {
        var (db, factory) = Create();
        var user = await SeedUser(db);
        var itemId = Guid.NewGuid();

        db.RunInventoryItems.Add(new RunInventoryItem { Id = Guid.NewGuid(), UserId = user.Id, ItemId = itemId });
        await db.SaveChangesAsync();

        var service = new InventoryService(factory);
        await service.ReturnFromRunAsync(user.Id);

        Assert.Single(db.InventoryItems);
        Assert.Empty(db.RunInventoryItems);
    }

    [Fact]
    public async Task MoveToMarket_ShouldMoveItem()
    {
        var (db, factory) = Create();
        var user = await SeedUser(db);
        var itemId = Guid.NewGuid();

        db.InventoryItems.Add(new InventoryItem { Id = Guid.NewGuid(), UserId = user.Id, ItemId = itemId });
        await db.SaveChangesAsync();

        var service = new InventoryService(factory);
        await service.MoveToMarketAsync(user.Id, itemId);

        Assert.Empty(db.InventoryItems);
        Assert.Single(db.MarketInventoryItems);
    }

    [Fact]
    public async Task ReturnFromMarket_ShouldRestoreInventory()
    {
        var (db, factory) = Create();
        var user = await SeedUser(db);
        var itemId = Guid.NewGuid();

        db.MarketInventoryItems.Add(new MarketInventoryItem { Id = Guid.NewGuid(), UserId = user.Id, ItemId = itemId });
        await db.SaveChangesAsync();

        var service = new InventoryService(factory);
        await service.ReturnFromMarketAsync(user.Id, itemId);

        Assert.Single(db.InventoryItems);
        Assert.Empty(db.MarketInventoryItems);
    }

    [Fact]
    public async Task TransferFromSellerToBuyer_ShouldMoveOwnership()
    {
        var (db, factory) = Create();
        var seller = await SeedUser(db);
        var buyer = await SeedUser(db);
        var itemId = Guid.NewGuid();

        db.MarketInventoryItems.Add(new MarketInventoryItem { Id = Guid.NewGuid(), UserId = seller.Id, ItemId = itemId });
        await db.SaveChangesAsync();

        var service = new InventoryService(factory);
        await service.TransferFromSellerToBuyerAsync(seller.Id, buyer.Id, itemId);

        Assert.Empty(db.MarketInventoryItems);
        Assert.Single(db.InventoryItems);
    }

    [Fact]
    public async Task MoveToRun_ShouldThrow_WhenItemNotInInventory()
    {
        var (db, factory) = Create();
        var user = await SeedUser(db);

        var service = new InventoryService(factory);
        await service.MoveToRunAsync(user.Id, new List<Guid> { Guid.NewGuid() });

        Assert.Empty(db.RunInventoryItems);
    }

    [Fact]
    public async Task EquipWeaponFromRun_ShouldEquip()
    {
        var (db, factory) = Create();
        var user = await SeedUser(db);
        var itemId = Guid.NewGuid();

        db.RunInventoryItems.Add(new RunInventoryItem { Id = Guid.NewGuid(), UserId = user.Id, ItemId = itemId });
        db.Weapons.Add(new Weapon { Id = itemId, Name = "Sword", Category = ItemCategory.Weapon, WeaponType = WeaponType.Sword });
        await db.SaveChangesAsync();

        var equipmentService = new EquipmentService(factory);
        await equipmentService.EquipWeaponFromRunAsync(user.Id, itemId, 1);

        var eq = await db.Equipments.AsNoTracking().FirstAsync(x => x.UserId == user.Id);
        Assert.Equal(itemId, eq.WeaponSlot1Id);
    }

    [Fact]
    public async Task EquipWeaponFromRun_ShouldThrow_WhenItemNotInRunInventory()
    {
        var (db, factory) = Create();
        var user = await SeedUser(db);
        var itemId = Guid.NewGuid();

        var equipmentService = new EquipmentService(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            equipmentService.EquipWeaponFromRunAsync(user.Id, itemId, 1));
    }

    [Fact]
    public async Task GetRunInventory_ShouldReturnItems()
    {
        var (db, factory) = Create();
        var user = await SeedUser(db);
        var itemId = Guid.NewGuid();

        db.RunInventoryItems.Add(new RunInventoryItem { Id = Guid.NewGuid(), UserId = user.Id, ItemId = itemId });
        await db.SaveChangesAsync();

        var service = new InventoryService(factory);
        var result = await service.GetRunInventoryAsync(user.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMarketInventory_ShouldReturnItems()
    {
        var (db, factory) = Create();
        var user = await SeedUser(db);
        var itemId = Guid.NewGuid();

        db.MarketInventoryItems.Add(new MarketInventoryItem { Id = Guid.NewGuid(), UserId = user.Id, ItemId = itemId });
        await db.SaveChangesAsync();

        var service = new InventoryService(factory);
        var result = await service.GetMarketInventoryAsync(user.Id);

        Assert.NotNull(result);
    }
}