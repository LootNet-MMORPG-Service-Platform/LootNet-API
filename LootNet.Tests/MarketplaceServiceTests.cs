using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.Enums;
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

    [Fact]
    public async Task GetListings_ReturnsAllListings_WithPaginationAndSort()
    {
        using var db = CreateDb();

        var user = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "seller", PasswordHash = "hash", Currency = 1000, Role = UserRole.Player };
        var weapon = new Weapon { Id = Guid.NewGuid(), OwnerId = user.Id, Name = "Sword", Category = ItemCategory.Weapon };
        var armor = new Armor { Id = Guid.NewGuid(), OwnerId = user.Id, Name = "Shield", Category = ItemCategory.Armor };
        var listing1 = new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = weapon.Id, Price = 100, Category = weapon.Category };
        var listing2 = new MarketListing { Id = Guid.NewGuid(), SellerId = user.Id, ItemId = armor.Id, Price = 200, Category = armor.Category };

        db.Users.Add(user);
        db.Weapons.Add(weapon);
        db.Armors.Add(armor);
        db.MarketListings.AddRange(listing1, listing2);
        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);

        var listingsAsc = await service.GetListingsAsync(pageNumber: 1, pageSize: 10, sort: "asc");
        var listingsDesc = await service.GetListingsAsync(pageNumber: 1, pageSize: 10, sort: "desc");

        Assert.Equal(2, listingsAsc.Count);
        Assert.Equal(100, listingsAsc[0].Price);
        Assert.Equal(200, listingsAsc[1].Price);

        Assert.Equal(2, listingsDesc.Count);
        Assert.Equal(200, listingsDesc[0].Price);
        Assert.Equal(100, listingsDesc[1].Price);
    }

    [Fact]
    public async Task CreateListing_CreatesListing_WhenItemOwned()
    {
        using var db = CreateDb();

        var user = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "player", PasswordHash = "hash", Currency = 500, Role = UserRole.Player };
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

        var user = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "player", PasswordHash = "hash", Currency = 500, Role = UserRole.Player };
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

        var seller = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "seller", PasswordHash = "hash", Currency = 1000, Role = UserRole.Player };
        var buyer = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "buyer", PasswordHash = "hash", Currency = 500, Role = UserRole.Player };
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

        var seller = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "seller", PasswordHash = "hash", Currency = 1000, Role = UserRole.Player };
        var buyer = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "buyer", PasswordHash = "hash", Currency = 100, Role = UserRole.Player };
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

        var buyer = new LootNet_API.Models.User { Id = Guid.NewGuid(), Username = "buyer", PasswordHash = "hash", Currency = 1000, Role = UserRole.Player };

        db.Users.Add(buyer);
        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BuyItemAsync(buyer.Id, Guid.NewGuid()));
    }
}