namespace LootNet_API.Tests;

using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Models.Market;
using LootNet_API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class MarketplaceServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
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

    [Fact]
    public async Task GetWeapons_ReturnsPagedSortedAndMappedCorrectly()
    {
        using var db = CreateDb();

        var user = CreateUser();

        var w1 = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "A",
            Category = ItemCategory.Weapon,
            Cut = 10,
            Blunt = 5,
            Elements = new List<ItemElement>()
        };

        var w2 = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "B",
            Category = ItemCategory.Weapon,
            Cut = 20,
            Blunt = 15,
            Elements = new List<ItemElement>()
        };

        var l1 = new MarketListing
        {
            Id = Guid.NewGuid(),
            ItemId = w1.Id,
            SellerId = user.Id,
            Price = 100,
            Category = ItemCategory.Weapon,
            IsSold = false
        };

        var l2 = new MarketListing
        {
            Id = Guid.NewGuid(),
            ItemId = w2.Id,
            SellerId = user.Id,
            Price = 200,
            Category = ItemCategory.Weapon,
            IsSold = false
        };

        db.Users.Add(user);
        db.Weapons.AddRange(w1, w2);
        db.MarketListings.AddRange(l1, l2);

        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);

        var asc = await service.GetWeaponsAsync(new WeaponQueryDTO
        {
            PageNumber = 1,
            PageSize = 10,
            SortColumn = WeaponSortColumns.Price,
            SortDirection = SortDirection.Asc
        });

        var desc = await service.GetWeaponsAsync(new WeaponQueryDTO
        {
            PageNumber = 1,
            PageSize = 10,
            SortColumn = WeaponSortColumns.Price,
            SortDirection = SortDirection.Desc
        });

        Assert.Equal(2, asc.Items.Count);
        Assert.Equal(2, desc.Items.Count);

        Assert.Equal(100, asc.Items[0].Price);
        Assert.Equal(200, asc.Items[1].Price);
        Assert.Equal(200, desc.Items[0].Price);
        Assert.Equal(100, desc.Items[1].Price);
    }

    [Fact]
    public async Task GetArmors_ReturnsPagedSortedCorrectly()
    {
        using var db = CreateDb();

        var user = CreateUser();

        var a1 = new Armor
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "A",
            Category = ItemCategory.Armor,
            CutResistance = 10,
            BluntResistance = 5,
            Elements = new List<ItemElement>()
        };

        var a2 = new Armor
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "B",
            Category = ItemCategory.Armor,
            CutResistance = 20,
            BluntResistance = 15,
            Elements = new List<ItemElement>()
        };

        var l1 = new MarketListing
        {
            Id = Guid.NewGuid(),
            ItemId = a1.Id,
            SellerId = user.Id,
            Price = 100,
            Category = ItemCategory.Armor,
            IsSold = false
        };

        var l2 = new MarketListing
        {
            Id = Guid.NewGuid(),
            ItemId = a2.Id,
            SellerId = user.Id,
            Price = 200,
            Category = ItemCategory.Armor,
            IsSold = false
        };

        db.Users.Add(user);
        db.Armors.AddRange(a1, a2);
        db.MarketListings.AddRange(l1, l2);

        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);

        var result = await service.GetArmorsAsync(new ArmorQueryDTO
        {
            PageNumber = 1,
            PageSize = 10,
            SortColumn = ArmorSortColumns.CutResistance,
            SortDirection = SortDirection.Desc
        });

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetWeapons_ReturnsEmpty_WhenNoData()
    {
        using var db = CreateDb();

        var service = new MarketplaceService(db);

        var result = await service.GetWeaponsAsync(new WeaponQueryDTO());

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetArmors_ReturnsEmpty_WhenNoData()
    {
        using var db = CreateDb();

        var service = new MarketplaceService(db);

        var result = await service.GetArmorsAsync(new ArmorQueryDTO());

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetWeapons_Throws_OnInvalidRange()
    {
        using var db = CreateDb();

        var service = new MarketplaceService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetWeaponsAsync(new WeaponQueryDTO
            {
                Cut = new RangeFilter<double>
                {
                    Min = 100,
                    Max = 10
                }
            })
        );
    }

    [Fact]
    public async Task GetArmors_Throws_OnInvalidRange()
    {
        using var db = CreateDb();

        var service = new MarketplaceService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetArmorsAsync(new ArmorQueryDTO
            {
                CutResistance = new RangeFilter<double>
                {
                    Min = 50,
                    Max = 10
                }
            })
        );
    }

    [Fact]
    public async Task GetWeapons_Pagination_WorksCorrectly()
    {
        using var db = CreateDb();

        var user = CreateUser();

        var listings = new List<MarketListing>();

        for (int i = 0; i < 25; i++)
        {
            var w = new Weapon
            {
                Id = Guid.NewGuid(),
                OwnerId = user.Id,
                Name = $"W{i}",
                Category = ItemCategory.Weapon
            };

            db.Weapons.Add(w);

            listings.Add(new MarketListing
            {
                Id = Guid.NewGuid(),
                ItemId = w.Id,
                SellerId = user.Id,
                Price = i,
                Category = ItemCategory.Weapon,
                IsSold = false
            });
        }

        db.Users.Add(user);
        db.MarketListings.AddRange(listings);

        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);

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
    public async Task GetWeapons_Filters_ByNameSearch()
    {
        using var db = CreateDb();

        var user = CreateUser();

        var w1 = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Dragon Sword",
            Category = ItemCategory.Weapon
        };

        var w2 = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Iron Axe",
            Category = ItemCategory.Weapon
        };

        db.Users.Add(user);
        db.Weapons.AddRange(w1, w2);

        db.MarketListings.AddRange(
            new MarketListing
            {
                Id = Guid.NewGuid(),
                ItemId = w1.Id,
                SellerId = user.Id,
                Price = 100,
                Category = ItemCategory.Weapon,
                IsSold = false
            },
            new MarketListing
            {
                Id = Guid.NewGuid(),
                ItemId = w2.Id,
                SellerId = user.Id,
                Price = 200,
                Category = ItemCategory.Weapon,
                IsSold = false
            }
        );

        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);

        var result = await service.GetWeaponsAsync(new WeaponQueryDTO
        {
            Search = "dragon"
        });

        Assert.Single(result.Items);
        Assert.Contains("Dragon", result.Items[0].Name);
    }

    [Fact]
    public async Task GetWeapons_IgnoresInvalidListingWithoutItem()
    {
        using var db = CreateDb();

        var listing = new MarketListing
        {
            Id = Guid.NewGuid(),
            ItemId = Guid.NewGuid(),
            Price = 100,
            Category = ItemCategory.Weapon,
            SellerId = Guid.NewGuid()
        };

        db.MarketListings.Add(listing);
        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);

        var result = await service.GetWeaponsAsync(new WeaponQueryDTO());

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetWeapons_ReturnsCorrectElements()
    {
        using var db = CreateDb();

        var user = CreateUser();

        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Fire Sword",
            Category = ItemCategory.Weapon,
            Elements = new List<ItemElement>
        {
            new ItemElement { ItemElementType = ItemElementType.Fire, Value = 10 }
        }
        };

        db.Users.Add(user);
        db.Weapons.Add(weapon);

        db.MarketListings.Add(new MarketListing
        {
            Id = Guid.NewGuid(),
            ItemId = weapon.Id,
            SellerId = user.Id,
            Price = 100,
            Category = ItemCategory.Weapon,
            IsSold = false
        });

        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);

        var result = await service.GetWeaponsAsync(new WeaponQueryDTO());

        Assert.Single(result.Items);
        Assert.Single(result.Items[0].Elements);
    }

    [Fact]
    public async Task GetArmors_ReturnsCorrectElements()
    {
        using var db = CreateDb();

        var user = CreateUser();

        var armor = new Armor
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Fire Armor",
            Category = ItemCategory.Armor,
            Elements = new List<ItemElement>
        {
            new ItemElement { ItemElementType = ItemElementType.Fire, Value = 5 }
        }
        };

        var listing = new MarketListing
        {
            Id = Guid.NewGuid(),
            ItemId = armor.Id,
            SellerId = user.Id,
            Price = 100,
            Category = ItemCategory.Armor,
            IsSold = false
        };

        db.Users.Add(user);
        db.Armors.Add(armor);
        db.MarketListings.Add(listing);

        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);

        var result = await service.GetArmorsAsync(new ArmorQueryDTO());

        Assert.Single(result.Items);
        Assert.Single(result.Items[0].Elements);
    }

    [Fact]
    public async Task CreateListing_CreatesListing_WhenItemOwned()
    {
        using var db = CreateDb();

        var user = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "player", PasswordHash = "hash", 
            Currency = 500, Role = UserRole.Player, Equipment = new Equipment()};
        var weapon = new Weapon { Id = Guid.NewGuid(), OwnerId = user.Id, Name = "Axe", Category = ItemCategory.Weapon };

        db.Users.Add(user);
        db.Weapons.Add(weapon);
        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);
        var dto = new CreateMarketListingDTO { ItemId = weapon.Id, Price = 150 };

        var listing = await service.CreateListingAsync(user.Id, dto);

        Assert.NotNull(listing);
        Assert.Equal(user.Id, listing.SellerId);
        Assert.Equal(weapon.Id, listing.ItemId);
        Assert.Equal(150, listing.Price);
        Assert.Equal(ItemCategory.Weapon, listing.Category);
    }

    [Fact]
    public async Task CreateListing_Throws_WhenItemNotOwned()
    {
        using var db = CreateDb();

        var user = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "player", PasswordHash = "hash",
            Currency = 500, Role = UserRole.Player, Equipment = new Equipment()
        };
        var weapon = new Weapon { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Name = "Axe", Category = ItemCategory.Weapon };

        db.Users.Add(user);
        db.Weapons.Add(weapon);
        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);
        var dto = new CreateMarketListingDTO { ItemId = weapon.Id, Price = 150 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateListingAsync(user.Id, dto));
    }

    [Fact]
    public async Task BuyItem_TransfersCurrencyAndOwnership_WhenSuccessful()
    {
        using var db = CreateDb();

        var seller = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "seller", PasswordHash = "hash",
            Currency = 1000, Role = UserRole.Player, Equipment = new Equipment()
        };
        var buyer = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "buyer", PasswordHash = "hash",
            Currency = 500, Role = UserRole.Player, Equipment = new Equipment()
        };
        var weapon = new Weapon { Id = Guid.NewGuid(), OwnerId = seller.Id, Name = "Sword", Category = ItemCategory.Weapon };
        var listing = new MarketListing { Id = Guid.NewGuid(), SellerId = seller.Id, ItemId = weapon.Id, Price = 300, Category = weapon.Category };

        db.Users.AddRange(seller, buyer);
        db.Weapons.Add(weapon);
        db.MarketListings.Add(listing);
        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);

        await service.BuyItemAsync(buyer.Id, listing.Id);

        Assert.Equal(200, buyer.Currency);
        Assert.Equal(1300, seller.Currency);
        Assert.Equal(buyer.Id, weapon.OwnerId);
        Assert.True(listing.IsSold);
    }

    [Fact]
    public async Task BuyItem_Throws_WhenNotEnoughCurrency()
    {
        using var db = CreateDb();

        var seller = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "seller", PasswordHash = "hash",
            Currency = 1000, Role = UserRole.Player, Equipment = new Equipment()
        };
        var buyer = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "buyer", PasswordHash = "hash",
            Currency = 100, Role = UserRole.Player, Equipment = new Equipment()
        };
        var weapon = new Weapon { Id = Guid.NewGuid(), OwnerId = seller.Id, Name = "Sword", Category = ItemCategory.Weapon };
        var listing = new MarketListing { Id = Guid.NewGuid(), SellerId = seller.Id, ItemId = weapon.Id, Price = 300, Category = weapon.Category };

        db.Users.AddRange(seller, buyer);
        db.Weapons.Add(weapon);
        db.MarketListings.Add(listing);
        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BuyItemAsync(buyer.Id, listing.Id));
    }

    [Fact]
    public async Task BuyItem_Throws_WhenListingNotFound()
    {
        using var db = CreateDb();

        var buyer = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "buyer", PasswordHash = "hash",
            Currency = 1000, Role = UserRole.Player, Equipment = new Equipment()
        };

        db.Users.Add(buyer);
        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BuyItemAsync(buyer.Id, Guid.NewGuid()));
    }
}