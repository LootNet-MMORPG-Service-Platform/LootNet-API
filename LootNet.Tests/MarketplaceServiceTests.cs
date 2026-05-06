namespace LootNet_API.Tests;

using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.DTO.Items;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Models.Market;
using LootNet_API.Services;
using LootNet_API.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class MarketplaceServiceTests
{
    private (AppDbContext db, SingleContextFactory factory) Create()
        => DbHelper.Create();

    private User CreateUser(decimal currency = 1000) => new User
    {
        Id = Guid.NewGuid(),
        Username = $"user_{Guid.NewGuid()}",
        PasswordHash = "hash",
        Currency = currency,
        Role = UserRole.Player,
        Equipment = new Equipment { Id = Guid.NewGuid() }
    };

    private MarketplaceService CreateService(AppDbContext db, SingleContextFactory factory)
        => new MarketplaceService(db, new InventoryService(factory));

    private void AddInventory(AppDbContext db, Guid userId, Guid itemId)
    {
        db.InventoryItems.Add(new InventoryItem { Id = Guid.NewGuid(), UserId = userId, ItemId = itemId });
    }

    [Fact]
    public async Task CreateListing_CreatesListing_WithCorrectData()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        var weapon = new Weapon { Id = Guid.NewGuid(), Name = "Sword", Category = ItemCategory.Weapon };

        db.Users.Add(user);
        db.Weapons.Add(weapon);
        AddInventory(db, user.Id, weapon.Id);
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var result = await service.CreateListingAsync(user.Id, new CreateMarketListingDTO { ItemId = weapon.Id, Price = 150 });

        Assert.NotNull(result);
        Assert.Single(db.MarketListings);
        Assert.Equal(weapon.Id, result.ItemId);
        Assert.Equal(150, result.Price);
    }

    [Fact]
    public async Task CreateListing_Throws_WhenItemNotFound()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateListingAsync(user.Id, new CreateMarketListingDTO { ItemId = Guid.NewGuid(), Price = 150 }));
    }

    [Fact]
    public async Task BuyItem_TransfersCurrency_AndInventory()
    {
        var (db, factory) = Create();
        var seller = CreateUser();
        var buyer = CreateUser(currency: 500);

        var weapon = new Weapon { Id = Guid.NewGuid(), Name = "Sword", Category = ItemCategory.Weapon, Elements = new List<ItemElement>() };

        db.Users.AddRange(seller, buyer);
        db.Weapons.Add(weapon);
        await db.SaveChangesAsync();

        var inventoryService = new InventoryService(factory);
        var marketplaceService = new MarketplaceService(db, inventoryService);

        await inventoryService.AddToInventoryAsync(seller.Id, weapon.Id);
        await inventoryService.MoveToMarketAsync(seller.Id, weapon.Id);

        var listing = await marketplaceService.CreateListingAsync(seller.Id, new CreateMarketListingDTO { ItemId = weapon.Id, Price = 300 });
        await marketplaceService.BuyItemAsync(buyer.Id, listing.Id);

        var updatedBuyer = await db.Users.FirstAsync(x => x.Id == buyer.Id);
        var updatedSeller = await db.Users.FirstAsync(x => x.Id == seller.Id);
        var updatedListing = await db.MarketListings.FirstAsync(x => x.Id == listing.Id);
        var buyerItems = await db.InventoryItems.Where(x => x.UserId == buyer.Id).ToListAsync();

        Assert.Equal(200, updatedBuyer.Currency);
        Assert.Equal(1300, updatedSeller.Currency);
        Assert.True(updatedListing.IsSold);
        Assert.Single(buyerItems);
    }

    [Fact]
    public async Task BuyItem_Throws_WhenNotEnoughCurrency()
    {
        var (db, factory) = Create();
        var seller = CreateUser();
        var buyer = CreateUser(currency: 100);

        var weapon = new Weapon { Id = Guid.NewGuid(), Name = "Sword", Category = ItemCategory.Weapon };

        db.Users.AddRange(seller, buyer);
        db.Weapons.Add(weapon);
        AddInventory(db, seller.Id, weapon.Id);
        db.MarketListings.Add(new MarketListing { Id = Guid.NewGuid(), SellerId = seller.Id, ItemId = weapon.Id, Price = 300, Category = ItemCategory.Weapon });
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuyItemAsync(buyer.Id, db.MarketListings.First().Id));
    }

    [Fact]
    public async Task BuyItem_Throws_WhenListingMissing()
    {
        var (db, factory) = Create();
        var buyer = CreateUser();

        db.Users.Add(buyer);
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BuyItemAsync(buyer.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetWeapons_ReturnsEmpty_WhenNoListings()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db, factory);

        var result = await service.GetWeaponsAsync(user.Id, new WeaponQueryDTO());

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetArmors_ReturnsEmpty_WhenNoListings()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db, factory);

        var result = await service.GetArmorsAsync(user.Id, new ArmorQueryDTO());

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetWeapons_Sorts_ByPrice()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        var viewer = CreateUser();
        var w1 = new Weapon { Id = Guid.NewGuid(), Name = "A", Category = ItemCategory.Weapon };
        var w2 = new Weapon { Id = Guid.NewGuid(), Name = "B", Category = ItemCategory.Weapon };

        db.Users.AddRange(user, viewer);
        db.Weapons.AddRange(w1, w2);
        AddInventory(db, user.Id, w1.Id);
        AddInventory(db, user.Id, w2.Id);
        db.MarketListings.AddRange(
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = w1.Id, Price = 100, Category = ItemCategory.Weapon },
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = w2.Id, Price = 200, Category = ItemCategory.Weapon }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var result = await service.GetWeaponsAsync(viewer.Id, new WeaponQueryDTO { SortColumn = WeaponSortColumns.Price, SortDirection = SortDirection.Asc });

        Assert.Equal(100, result.Items.First().Price);
    }

    [Fact]
    public async Task GetWeapons_Pagination_Works()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        var viewer = CreateUser();

        db.Users.AddRange(user, viewer);

        for (int i = 0; i < 20; i++)
        {
            var w = new Weapon { Id = Guid.NewGuid(), Name = $"W{i}", Category = ItemCategory.Weapon };
            db.Weapons.Add(w);
            AddInventory(db, user.Id, w.Id);
            db.MarketListings.Add(new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = w.Id, Price = i, Category = ItemCategory.Weapon });
        }
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var page1 = await service.GetWeaponsAsync(viewer.Id, new WeaponQueryDTO { PageNumber = 1, PageSize = 10 });
        var page2 = await service.GetWeaponsAsync(viewer.Id, new WeaponQueryDTO { PageNumber = 2, PageSize = 10 });

        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(10, page2.Items.Count);
    }

    [Fact]
    public async Task GetWeapons_Filters_ByName()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        var viewer = CreateUser();
        var w1 = new Weapon { Id = Guid.NewGuid(), Name = "Dragon Sword", Category = ItemCategory.Weapon };
        var w2 = new Weapon { Id = Guid.NewGuid(), Name = "Iron Axe", Category = ItemCategory.Weapon };

        db.Users.AddRange(user, viewer);
        db.Weapons.AddRange(w1, w2);
        AddInventory(db, user.Id, w1.Id);
        AddInventory(db, user.Id, w2.Id);
        db.MarketListings.AddRange(
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = w1.Id, Price = 100, Category = ItemCategory.Weapon },
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = w2.Id, Price = 200, Category = ItemCategory.Weapon }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var result = await service.GetWeaponsAsync(viewer.Id, new WeaponQueryDTO { Search = "dragon" });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetWeapons_Throws_OnInvalidRange()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db, factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetWeaponsAsync(user.Id, new WeaponQueryDTO { Cut = new RangeFilter<double> { Min = 100, Max = 10 } }));
    }

    [Fact]
    public async Task GetArmors_Throws_OnInvalidRange()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db, factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetArmorsAsync(user.Id, new ArmorQueryDTO { CutResistance = new RangeFilter<double> { Min = 100, Max = 10 } }));
    }

    [Fact]
    public async Task GetMyListings_ReturnsOnlyActiveListingsOfUser()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        var other = CreateUser();
        var weaponA = new Weapon { Id = Guid.NewGuid(), Name = "A", Category = ItemCategory.Weapon };
        var weaponB = new Weapon { Id = Guid.NewGuid(), Name = "B", Category = ItemCategory.Weapon };

        db.Users.AddRange(user, other);
        db.Weapons.AddRange(weaponA, weaponB);
        db.MarketListings.AddRange(
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = weaponA.Id, Price = 100, Category = ItemCategory.Weapon, IsSold = false },
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = weaponB.Id, Price = 120, Category = ItemCategory.Weapon, IsSold = true },
            new MarketListing { Id = Guid.NewGuid(), SellerId = other.Id, ItemId = weaponB.Id, Price = 130, Category = ItemCategory.Weapon, IsSold = false }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var result = await service.GetMyListingsAsync(user.Id);

        Assert.Single(result);
        Assert.Equal("A", result[0].Name);
    }

    [Fact]
    public async Task GetMyTransactions_ReturnsSaleAndPurchaseWithCounterparty()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        var buyer = CreateUser();
        var seller = CreateUser();
        var weapon = new Weapon { Id = Guid.NewGuid(), Name = "Dragon Sword", Category = ItemCategory.Weapon };

        db.Users.AddRange(user, buyer, seller);
        db.Weapons.Add(weapon);
        db.Transactions.AddRange(
            new Transaction { Id = Guid.NewGuid(), BuyerId = buyer.Id, SellerId = user.Id, ItemId = weapon.Id, Price = 200, Timestamp = DateTime.UtcNow.AddMinutes(-5) },
            new Transaction { Id = Guid.NewGuid(), BuyerId = user.Id, SellerId = seller.Id, ItemId = weapon.Id, Price = 150, Timestamp = DateTime.UtcNow.AddMinutes(-1) }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var result = await service.GetMyTransactionsAsync(user.Id);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.IsSale && x.CounterpartyUsername == buyer.Username);
        Assert.Contains(result, x => !x.IsSale && x.CounterpartyUsername == seller.Username);
    }
}
