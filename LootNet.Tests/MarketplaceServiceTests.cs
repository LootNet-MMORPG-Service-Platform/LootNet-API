namespace LootNet_API.Tests;

using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.DTO.Items;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Models.Market;
using LootNet_API.Services;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class MarketplaceServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = "user",
            PasswordHash = "hash",
            Currency = 1000,
            Role = UserRole.Player,
            Equipment = new Equipment()
        };
    }

    private IInventoryService CreateInventory(AppDbContext db)
        => new InventoryService(db);

    private MarketplaceService CreateService(AppDbContext db)
        => new MarketplaceService(db, CreateInventory(db));

    private void AddInventory(AppDbContext db, Guid userId, Guid itemId)
    {
        db.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemId = itemId
        });
    }

    [Fact]
    public async Task CreateListing_MovesItemFromInventory_ToMarket()
    {
        using var db = CreateDb();
        var user = CreateUser();

        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            Name = "Sword",
            Category = ItemCategory.Weapon
        };

        db.Users.Add(user);
        db.Weapons.Add(weapon);
        AddInventory(db, user.Id, weapon.Id);

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.CreateListingAsync(user.Id,
            new CreateMarketListingDTO
            {
                ItemId = weapon.Id,
                Price = 150
            });

        Assert.NotNull(result);
        Assert.Single(db.MarketListings);
        Assert.Empty(db.InventoryItems);
    }

    [Fact]
    public async Task CreateListing_Throws_WhenItemNotInInventory()
    {
        using var db = CreateDb();
        var user = CreateUser();

        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            Name = "Sword",
            Category = ItemCategory.Weapon
        };

        db.Users.Add(user);
        db.Weapons.Add(weapon);

        await db.SaveChangesAsync();

        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateListingAsync(user.Id,
                new CreateMarketListingDTO
                {
                    ItemId = weapon.Id,
                    Price = 150
                }));
    }

    [Fact]
    public async Task BuyItem_TransfersCurrency_AndInventory()
    {
        using var db = CreateDb();

        var seller = CreateUser();
        var buyer = CreateUser();
        buyer.Currency = 500;

        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            Name = "Sword",
            Category = ItemCategory.Weapon,
            Elements = new List<ItemElement>()
        };

        db.Users.AddRange(seller, buyer);
        db.Weapons.Add(weapon);
        await db.SaveChangesAsync();

        var inventoryService = new InventoryService(db);
        var marketplaceService = new MarketplaceService(db, inventoryService);

        await inventoryService.AddToInventoryAsync(seller.Id, weapon.Id);

        var listing = await marketplaceService.CreateListingAsync(seller.Id, new CreateMarketListingDTO
        {
            ItemId = weapon.Id,
            Price = 300
        });

        await marketplaceService.BuyItemAsync(buyer.Id, listing.Id);

        var updatedBuyer = await db.Users.FirstAsync(x => x.Id == buyer.Id);
        var updatedSeller = await db.Users.FirstAsync(x => x.Id == seller.Id);
        var updatedListing = await db.MarketListings.FirstAsync(x => x.Id == listing.Id);

        var buyerItems = await db.InventoryItems
            .Where(x => x.UserId == buyer.Id)
            .ToListAsync();

        Assert.Equal(200, updatedBuyer.Currency);
        Assert.Equal(1300, updatedSeller.Currency);
        Assert.True(updatedListing.IsSold);
        Assert.Single(buyerItems);
    }

    [Fact]
    public async Task BuyItem_Throws_WhenNotEnoughCurrency()
    {
        using var db = CreateDb();
        var seller = CreateUser();
        var buyer = CreateUser();
        buyer.Currency = 100;

        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            Name = "Sword",
            Category = ItemCategory.Weapon
        };

        db.Users.AddRange(seller, buyer);
        db.Weapons.Add(weapon);
        AddInventory(db, seller.Id, weapon.Id);

        var listing = new MarketListing
        {
            Id = Guid.NewGuid(),
            SellerId = seller.Id,
            ItemId = weapon.Id,
            Price = 300,
            Category = ItemCategory.Weapon
        };

        db.MarketListings.Add(listing);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuyItemAsync(buyer.Id, listing.Id));
    }

    [Fact]
    public async Task BuyItem_Throws_WhenListingMissing()
    {
        using var db = CreateDb();
        var buyer = CreateUser();

        db.Users.Add(buyer);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuyItemAsync(buyer.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetWeapons_ReturnsEmpty_WhenNoListings()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.GetWeaponsAsync(new WeaponQueryDTO());

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetArmors_ReturnsEmpty_WhenNoListings()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.GetArmorsAsync(new ArmorQueryDTO());

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetWeapons_Sorts_ByPrice()
    {
        using var db = CreateDb();
        var user = CreateUser();

        var w1 = new Weapon { Id = Guid.NewGuid(), Name = "A", Category = ItemCategory.Weapon };
        var w2 = new Weapon { Id = Guid.NewGuid(), Name = "B", Category = ItemCategory.Weapon };

        db.Users.Add(user);
        db.Weapons.AddRange(w1, w2);
        AddInventory(db, user.Id, w1.Id);
        AddInventory(db, user.Id, w2.Id);

        db.MarketListings.AddRange(
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = w1.Id, Price = 100, Category = ItemCategory.Weapon },
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = w2.Id, Price = 200, Category = ItemCategory.Weapon }
        );

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.GetWeaponsAsync(new WeaponQueryDTO
        {
            SortColumn = WeaponSortColumns.Price,
            SortDirection = SortDirection.Asc
        });

        Assert.Equal(100, result.Items.First().Price);
    }

    [Fact]
    public async Task GetWeapons_Pagination_Works()
    {
        using var db = CreateDb();
        var user = CreateUser();

        db.Users.Add(user);

        for (int i = 0; i < 20; i++)
        {
            var w = new Weapon { Id = Guid.NewGuid(), Name = $"W{i}", Category = ItemCategory.Weapon };
            db.Weapons.Add(w);
            AddInventory(db, user.Id, w.Id);

            db.MarketListings.Add(new MarketListing
            {
                Id = Guid.NewGuid(),
                SellerId = user.Id,
                ItemId = w.Id,
                Price = i,
                Category = ItemCategory.Weapon
            });
        }

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var page1 = await service.GetWeaponsAsync(new WeaponQueryDTO
        {
            PageNumber = 1,
            PageSize = 10
        });

        var page2 = await service.GetWeaponsAsync(new WeaponQueryDTO
        {
            PageNumber = 2,
            PageSize = 10
        });

        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(10, page2.Items.Count);
    }

    [Fact]
    public async Task GetWeapons_Filters_ByName()
    {
        using var db = CreateDb();
        var user = CreateUser();

        var w1 = new Weapon { Id = Guid.NewGuid(), Name = "Dragon Sword", Category = ItemCategory.Weapon };
        var w2 = new Weapon { Id = Guid.NewGuid(), Name = "Iron Axe", Category = ItemCategory.Weapon };

        db.Users.Add(user);
        db.Weapons.AddRange(w1, w2);

        AddInventory(db, user.Id, w1.Id);
        AddInventory(db, user.Id, w2.Id);

        db.MarketListings.AddRange(
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = w1.Id, Price = 100, Category = ItemCategory.Weapon },
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = w2.Id, Price = 200, Category = ItemCategory.Weapon }
        );

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.GetWeaponsAsync(new WeaponQueryDTO
        {
            Search = "dragon"
        });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetWeapons_Throws_OnInvalidRange()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetWeaponsAsync(new WeaponQueryDTO
            {
                Cut = new RangeFilter<double> { Min = 100, Max = 10 }
            }));
    }

    [Fact]
    public async Task GetArmors_Throws_OnInvalidRange()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetArmorsAsync(new ArmorQueryDTO
            {
                CutResistance = new RangeFilter<double> { Min = 100, Max = 10 }
            }));
    }
}