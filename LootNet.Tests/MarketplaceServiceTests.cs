namespace LootNet_API.Tests;

using LootNet_API.Data;
using LootNet_API.Configuration;
using LootNet_API.DTO;
using LootNet_API.DTO.Market;
using LootNet_API.DTO.Items;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Models.Logs;
using LootNet_API.Models.Market;
using LootNet_API.Services;
using LootNet_API.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

public class MarketplaceServiceTests
{
    private (AppDbContext db, SingleContextFactory factory) Create()
        => DbHelper.Create();

    private User CreateUser(decimal currency = 1000)
    {
        var id = Guid.NewGuid();
        return new User
        {
            Id = id,
            Username = $"user_{Guid.NewGuid()}",
            PasswordHash = "hash",
            Currency = currency,
            Role = UserRole.Player,
            Equipment = new Equipment { Id = Guid.NewGuid(), UserId = id }
        };
    }

    private MarketplaceService CreateService(AppDbContext db, SingleContextFactory factory)
        => new MarketplaceService(db, new InventoryService(factory));

    private MarketplaceService CreateService(AppDbContext db, SingleContextFactory factory, MarketplaceEconomyOptions options)
        => new MarketplaceService(db, new InventoryService(factory), Options.Create(options));

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
        Assert.Empty(db.InventoryItems.Where(x => x.UserId == user.Id && x.ItemId == weapon.Id));
        Assert.Single(db.MarketInventoryItems.Where(x => x.UserId == user.Id && x.ItemId == weapon.Id));
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
    public async Task CreateListing_Throws_WhenPriceIsNotPositive()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        var weapon = new Weapon { Id = Guid.NewGuid(), Name = "Sword", Category = ItemCategory.Weapon };

        db.Users.Add(user);
        db.Weapons.Add(weapon);
        AddInventory(db, user.Id, weapon.Id);
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateListingAsync(user.Id, new CreateMarketListingDTO { ItemId = weapon.Id, Price = 0 }));
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

        var listing = await marketplaceService.CreateListingAsync(seller.Id, new CreateMarketListingDTO { ItemId = weapon.Id, Price = 300 });
        await marketplaceService.BuyItemAsync(buyer.Id, listing.Id);

        var updatedBuyer = await db.Users.FirstAsync(x => x.Id == buyer.Id);
        var updatedSeller = await db.Users.FirstAsync(x => x.Id == seller.Id);
        var updatedListing = await db.MarketListings.FirstAsync(x => x.Id == listing.Id);
        var buyerItems = await db.InventoryItems.Where(x => x.UserId == buyer.Id).ToListAsync();

        Assert.Equal(200, updatedBuyer.Currency);
        Assert.Equal(1285, updatedSeller.Currency);
        Assert.True(updatedListing.IsSold);
        Assert.Single(buyerItems);

        var transaction = await db.Transactions.SingleAsync();
        Assert.Equal(300, transaction.Price);
        Assert.Equal(15, transaction.TaxAmount);
        Assert.Equal(285, transaction.SellerPayout);
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
    public async Task GetWeapons_ContainsSellerInfo()
    {
        var (db, factory) = Create();
        var seller = CreateUser();
        var viewer = CreateUser();
        var w = new Weapon { Id = Guid.NewGuid(), Name = "Seller Sword", Category = ItemCategory.Weapon };
        db.Users.AddRange(seller, viewer);
        db.Weapons.Add(w);
        db.MarketListings.Add(new MarketListing { Id = Guid.NewGuid(), SellerId = seller.Id, ItemId = w.Id, Price = 100, Category = ItemCategory.Weapon });
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var result = await service.GetWeaponsAsync(viewer.Id, new WeaponQueryDTO { PageNumber = 1, PageSize = 10 });
        Assert.Equal(seller.Username, result.Items.First().SellerUsername);
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
        var result = await service.GetMyListingsAsync(user.Id, new MyListingsQueryDTO { PageNumber = 1, PageSize = 10 });

        Assert.Single(result.Items);
        Assert.Equal("A", result.Items[0].Name);
    }

    [Fact]
    public async Task GetListingsBySeller_ReturnsOnlyRequestedSeller()
    {
        var (db, factory) = Create();
        var s1 = CreateUser();
        var s2 = CreateUser();
        var w1 = new Weapon { Id = Guid.NewGuid(), Name = "A", Category = ItemCategory.Weapon };
        var w2 = new Weapon { Id = Guid.NewGuid(), Name = "B", Category = ItemCategory.Weapon };
        db.Users.AddRange(s1, s2);
        db.Weapons.AddRange(w1, w2);
        db.MarketListings.AddRange(
            new MarketListing { Id = Guid.NewGuid(), SellerId = s1.Id, ItemId = w1.Id, Price = 100, Category = ItemCategory.Weapon, IsSold = false },
            new MarketListing { Id = Guid.NewGuid(), SellerId = s2.Id, ItemId = w2.Id, Price = 120, Category = ItemCategory.Weapon, IsSold = false }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var result = await service.GetListingsBySellerAsync(s1.Id, new MyListingsQueryDTO { PageNumber = 1, PageSize = 10 });
        Assert.Single(result.Items);
        Assert.Equal("A", result.Items[0].Name);
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
        var result = await service.GetMyTransactionsAsync(user.Id, new MarketTransactionsQueryDTO { PageNumber = 1, PageSize = 10 });

        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, x => x.IsSale && x.CounterpartyUsername == buyer.Username);
        Assert.Contains(result.Items, x => !x.IsSale && x.CounterpartyUsername == seller.Username);
    }

    [Fact]
    public async Task ChangeListingPrice_UpdatesPrice_ForOwner()
    {
        var (db, factory) = Create();
        var seller = CreateUser();
        var weapon = new Weapon { Id = Guid.NewGuid(), Name = "Sword", Category = ItemCategory.Weapon };
        db.Users.Add(seller);
        db.Weapons.Add(weapon);
        db.MarketListings.Add(new MarketListing
        {
            Id = Guid.NewGuid(),
            SellerId = seller.Id,
            ItemId = weapon.Id,
            Price = 100,
            Category = ItemCategory.Weapon,
            IsSold = false
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var listingId = db.MarketListings.First().Id;
        await service.ChangeListingPriceAsync(seller.Id, listingId, 250);

        Assert.Equal(250, db.MarketListings.First().Price);
    }

    [Fact]
    public async Task CancelListing_RemovesListing_AndReturnsItemToInventory()
    {
        var (db, factory) = Create();
        var seller = CreateUser();
        var weapon = new Weapon { Id = Guid.NewGuid(), Name = "Sword", Category = ItemCategory.Weapon };
        db.Users.Add(seller);
        db.Weapons.Add(weapon);
        db.MarketListings.Add(new MarketListing
        {
            Id = Guid.NewGuid(),
            SellerId = seller.Id,
            ItemId = weapon.Id,
            Price = 150,
            Category = ItemCategory.Weapon,
            IsSold = false
        });
        db.MarketInventoryItems.Add(new MarketInventoryItem { Id = Guid.NewGuid(), UserId = seller.Id, ItemId = weapon.Id });
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var listingId = db.MarketListings.First().Id;
        await service.CancelListingAsync(seller.Id, listingId);

        Assert.Empty(db.MarketListings);
        Assert.Empty(db.MarketInventoryItems);
        Assert.Single(db.InventoryItems.Where(x => x.UserId == seller.Id && x.ItemId == weapon.Id));
    }

    [Fact]
    public async Task GetMyListingsSummary_ReturnsCountAndTotalValue()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        db.Users.Add(user);
        db.MarketListings.AddRange(
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = Guid.NewGuid(), Price = 100, Category = ItemCategory.Weapon, IsSold = false },
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = Guid.NewGuid(), Price = 250, Category = ItemCategory.Armor, IsSold = false },
            new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = Guid.NewGuid(), Price = 500, Category = ItemCategory.Armor, IsSold = true }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var summary = await service.GetMyListingsSummaryAsync(user.Id);

        Assert.Equal(2, summary.TotalItemsListed);
        Assert.Equal(350, summary.TotalListedValue);
    }

    [Fact]
    public async Task GetMyTransactionsSummary_ReturnsSoldAndBoughtTotals()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        var buyer = CreateUser();
        var seller = CreateUser();
        db.Users.AddRange(user, buyer, seller);
        db.Transactions.AddRange(
            new Transaction { Id = Guid.NewGuid(), BuyerId = buyer.Id, SellerId = user.Id, ItemId = Guid.NewGuid(), Price = 200, Timestamp = DateTime.UtcNow },
            new Transaction { Id = Guid.NewGuid(), BuyerId = user.Id, SellerId = seller.Id, ItemId = Guid.NewGuid(), Price = 120, Timestamp = DateTime.UtcNow },
            new Transaction { Id = Guid.NewGuid(), BuyerId = user.Id, SellerId = seller.Id, ItemId = Guid.NewGuid(), Price = 80, Timestamp = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var summary = await service.GetMyTransactionsSummaryAsync(user.Id);

        Assert.Equal(200, summary.TotalSold);
        Assert.Equal(200, summary.TotalBought);
        Assert.Equal(0, summary.Difference);
    }

    [Fact]
    public void CalculateSaleTax_UsesMarginalProgressiveBrackets()
    {
        var (db, factory) = Create();
        var service = CreateService(db, factory);

        var tax = service.CalculateSaleTax(12_000);

        Assert.Equal(12_000, tax.GrossPrice);
        Assert.Equal(2_115, tax.TaxAmount);
        Assert.Equal(9_885, tax.SellerPayout);
        Assert.Equal(0.1763m, tax.EffectiveTaxRate);
    }

    [Fact]
    public void CalculateSaleTax_Throws_WhenPriceIsNotPositive()
    {
        var (db, factory) = Create();
        var service = CreateService(db, factory);

        Assert.Throws<InvalidOperationException>(() => service.CalculateSaleTax(0));
    }

    [Fact]
    public async Task SellItemToBot_PaysStatBasedPrice_RemovesItem_AndRecordsTransaction()
    {
        var (db, factory) = Create();
        var user = CreateUser(currency: 100);
        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            Name = "Precise Sword",
            Category = ItemCategory.Weapon,
            Cut = 10,
            Blunt = 5,
            Elements = new List<ItemElement>
            {
                new() { Id = Guid.NewGuid(), ItemElementType = ItemElementType.Fire, Value = 2 }
            }
        };

        db.Users.Add(user);
        db.Weapons.Add(weapon);
        AddInventory(db, user.Id, weapon.Id);
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var result = await service.SellItemToBotAsync(user.Id, weapon.Id);

        Assert.Equal(164, result.PaidAmount);
        Assert.Equal(264, result.CurrencyAfterSale);
        Assert.Empty(db.InventoryItems.Where(x => x.UserId == user.Id && x.ItemId == weapon.Id));
        Assert.Empty(db.Weapons.Where(x => x.Id == weapon.Id));

        var transaction = await db.Transactions.SingleAsync();
        Assert.Equal(user.Id, transaction.SellerId);
        Assert.Equal(164, transaction.Price);
        Assert.Equal(164, transaction.SellerPayout);
        Assert.Equal(0, transaction.TaxAmount);
    }

    [Fact]
    public async Task GetBotSaleOffer_ReturnsStatBasedPrice_WithoutMutatingState()
    {
        var (db, factory) = Create();
        var user = CreateUser(currency: 100);
        var armor = new Armor
        {
            Id = Guid.NewGuid(),
            Name = "Layered Chest",
            Category = ItemCategory.Armor,
            ArmorType = ArmorType.Chestplate,
            CutResistance = 7,
            BluntResistance = 3
        };

        db.Users.Add(user);
        db.Armors.Add(armor);
        AddInventory(db, user.Id, armor.Id);
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var offer = await service.GetBotSaleOfferAsync(user.Id, armor.Id);

        Assert.Equal(100, offer.OfferedPrice);
        Assert.Equal(100, user.Currency);
        Assert.Single(db.InventoryItems.Where(x => x.UserId == user.Id && x.ItemId == armor.Id));
        Assert.Empty(db.Transactions);
    }

    [Fact]
    public async Task EconomySettings_ControlBotPriceAndTaxBrackets()
    {
        var (db, factory) = Create();
        var user = CreateUser(currency: 100);
        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            Name = "Config Sword",
            Category = ItemCategory.Weapon,
            Cut = 10,
            Blunt = 5
        };
        var options = new MarketplaceEconomyOptions
        {
            DailyCurrencyReward = 33,
            BotBasePrice = 10,
            BotStatMultiplier = 2,
            BotElementMultiplier = 1,
            ProgressiveTaxBrackets = new List<MarketplaceTaxBracketOptions>
            {
                new() { From = 0, To = 100, Rate = 0.10m },
                new() { From = 100, To = null, Rate = 0.20m }
            }
        };

        db.Users.Add(user);
        db.Weapons.Add(weapon);
        AddInventory(db, user.Id, weapon.Id);
        await db.SaveChangesAsync();

        var service = CreateService(db, factory, options);
        var economy = service.GetEconomy();
        var offer = await service.GetBotSaleOfferAsync(user.Id, weapon.Id);
        var tax = service.CalculateSaleTax(150);

        Assert.Equal(33, economy.DailyCurrencyReward);
        Assert.Equal(40, offer.OfferedPrice);
        Assert.Equal(20, tax.TaxAmount);
        Assert.Equal(130, tax.SellerPayout);
    }

    [Fact]
    public async Task SellItemToBot_AppliesProgressiveTax_WhenEnabled()
    {
        var (db, factory) = Create();
        var user = CreateUser(currency: 100);
        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            Name = "Taxed Bot Sword",
            Category = ItemCategory.Weapon,
            Cut = 10,
            Blunt = 5
        };
        var options = new MarketplaceEconomyOptions
        {
            DailyCurrencyReward = 75,
            BotBasePrice = 0,
            BotStatMultiplier = 10,
            BotElementMultiplier = 1,
            IsPlayerToPlayerTaxEnabled = true,
            IsPlayerToBotTaxEnabled = true,
            ProgressiveTaxBrackets = new List<MarketplaceTaxBracketOptions>
            {
                new() { From = 0, To = 100, Rate = 0.10m },
                new() { From = 100, To = null, Rate = 0.20m }
            }
        };

        db.Users.Add(user);
        db.Weapons.Add(weapon);
        AddInventory(db, user.Id, weapon.Id);
        await db.SaveChangesAsync();

        var service = CreateService(db, factory, options);
        var result = await service.SellItemToBotAsync(user.Id, weapon.Id);

        Assert.Equal(130, result.PaidAmount);
        Assert.Equal(230, result.CurrencyAfterSale);

        var transaction = await db.Transactions.SingleAsync();
        Assert.Equal(150, transaction.Price);
        Assert.Equal(20, transaction.TaxAmount);
        Assert.Equal(130, transaction.SellerPayout);
    }

    [Fact]
    public async Task SellItemToBot_Throws_WhenItemIsNotInInventory()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        var weapon = new Weapon { Id = Guid.NewGuid(), Name = "Sword", Category = ItemCategory.Weapon };
        db.Users.Add(user);
        db.Weapons.Add(weapon);
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SellItemToBotAsync(user.Id, weapon.Id));
    }

    [Fact]
    public async Task SellItemToBot_Throws_WhenItemIsEquipped()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        var weapon = new Weapon { Id = Guid.NewGuid(), Name = "Sword", Category = ItemCategory.Weapon };
        user.Equipment.WeaponSlot1Id = weapon.Id;
        db.Users.Add(user);
        db.Weapons.Add(weapon);
        AddInventory(db, user.Id, weapon.Id);
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SellItemToBotAsync(user.Id, weapon.Id));
    }

    [Fact]
    public async Task CreateListing_Throws_WhenItemIsEquipped()
    {
        var (db, factory) = Create();
        var user = CreateUser();
        var weapon = new Weapon { Id = Guid.NewGuid(), Name = "Sword", Category = ItemCategory.Weapon };
        user.Equipment.WeaponSlot1Id = weapon.Id;
        db.Users.Add(user);
        db.Weapons.Add(weapon);
        AddInventory(db, user.Id, weapon.Id);
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateListingAsync(user.Id, new CreateMarketListingDTO { ItemId = weapon.Id, Price = 100 }));
    }

    [Fact]
    public void GetEconomy_DeduplicatesProgressiveTaxBrackets_FromAdminSettings()
    {
        var (db, factory) = Create();
        db.AdminLogs.Add(new AdminLog
        {
            Id = Guid.NewGuid(),
            AdminId = Guid.NewGuid(),
            Action = "UPDATE_MARKETPLACE_ECONOMY",
            TargetUserId = Guid.NewGuid().ToString(),
            Data = """
            {
              "DailyCurrencyReward": 10,
              "BotBasePrice": 50,
              "BotStatMultiplier": 2,
              "BotElementMultiplier": 1,
              "IsPlayerToPlayerTaxEnabled": true,
              "IsPlayerToBotTaxEnabled": true,
              "ProgressiveTaxBrackets": [
                { "From": 0, "To": 500, "Rate": 0.10 },
                { "From": 0, "To": 500, "Rate": 0.10 },
                { "From": 500, "To": null, "Rate": 0.20 }
              ]
            }
            """
        });
        db.SaveChanges();

        var service = CreateService(db, factory);
        var economy = service.GetEconomy();

        Assert.Equal(2, economy.ProgressiveTaxBrackets.Count);
        Assert.Equal(0, economy.ProgressiveTaxBrackets[0].From);
        Assert.Equal(500, economy.ProgressiveTaxBrackets[0].To);
        Assert.Equal(500, economy.ProgressiveTaxBrackets[1].From);
        Assert.Null(economy.ProgressiveTaxBrackets[1].To);
    }

    [Fact]
    public void CalculateSaleTax_DoesNotDoubleCount_OverlappingDuplicateBrackets()
    {
        var (db, factory) = Create();
        db.AdminLogs.Add(new AdminLog
        {
            Id = Guid.NewGuid(),
            AdminId = Guid.NewGuid(),
            Action = "UPDATE_MARKETPLACE_ECONOMY",
            TargetUserId = Guid.NewGuid().ToString(),
            Data = """
            {
              "DailyCurrencyReward": 10,
              "BotBasePrice": 50,
              "BotStatMultiplier": 2,
              "BotElementMultiplier": 1,
              "IsPlayerToPlayerTaxEnabled": true,
              "IsPlayerToBotTaxEnabled": true,
              "ProgressiveTaxBrackets": [
                { "From": 0, "To": 500, "Rate": 0.05 },
                { "From": 0, "To": 500, "Rate": 0.0500000 },
                { "From": 500, "To": 2000, "Rate": 0.10 }
              ]
            }
            """
        });
        db.SaveChanges();

        var service = CreateService(db, factory);
        var tax = service.CalculateSaleTax(500);

        Assert.Equal(25, tax.TaxAmount);
        Assert.Equal(475, tax.SellerPayout);
        Assert.Equal(0.05m, tax.EffectiveTaxRate);
    }

    [Fact]
    public async Task GetSellInventory_FiltersSortsAndExcludesEquipped()
    {
        var (db, factory) = Create();
        var user = CreateUser();

        var w1 = new Weapon { Id = Guid.NewGuid(), Name = "Alpha Sword", Category = ItemCategory.Weapon, Cut = 8, Blunt = 2 };
        var w2 = new Weapon { Id = Guid.NewGuid(), Name = "Beta Polearm", Category = ItemCategory.Weapon, Cut = 20, Blunt = 5 };
        var a1 = new Armor { Id = Guid.NewGuid(), Name = "Crude Helmet", Category = ItemCategory.Armor, CutResistance = 3, BluntResistance = 1 };

        user.Equipment.WeaponSlot1Id = w2.Id;
        db.Users.Add(user);
        db.Weapons.AddRange(w1, w2);
        db.Armors.Add(a1);
        AddInventory(db, user.Id, w1.Id);
        AddInventory(db, user.Id, w2.Id);
        AddInventory(db, user.Id, a1.Id);
        await db.SaveChangesAsync();

        var service = CreateService(db, factory);
        var result = await service.GetSellInventoryAsync(user.Id, new SellInventoryQueryDTO
        {
            ItemType = "weapon",
            Search = "alpha",
            SortBy = "power",
            SortDirection = SortDirection.Desc,
            PageNumber = 1,
            PageSize = 10
        });

        Assert.Single(result.Items);
        Assert.Equal(w1.Id, result.Items[0].Id);
        Assert.Equal("weapon", result.Items[0].ItemKind);
        Assert.DoesNotContain(result.Items, x => x.Id == w2.Id);
    }
}
