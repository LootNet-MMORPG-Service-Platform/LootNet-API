using LootNet_API.Controllers;
using LootNet_API.DTO;
using LootNet_API.Enums;
using LootNet_API.Models.Market;
using LootNet_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

public class MarketplaceControllerTests
{
    private readonly Mock<IMarketplaceService> _marketServiceMock;
    private readonly MarketplaceController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public MarketplaceControllerTests()
    {
        _marketServiceMock = new Mock<IMarketplaceService>();
        _controller = new MarketplaceController(_marketServiceMock.Object);

        var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(System.Security.Claims.ClaimTypes.NameIdentifier, _userId.ToString())
        }));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };
    }

    [Fact]
    public async Task GetMarket_ReturnsListings()
    {
        var listings = new List<MarketListingDTO>
        {
            new MarketListingDTO { Id = Guid.NewGuid(), ItemName = "Sword", Price = 100, SellerId = _userId, Category = ItemCategory.Weapon }
        };

        _marketServiceMock
            .Setup(s => s.GetListingsAsync(null, 1, 20, "asc"))
            .ReturnsAsync(listings);

        var result = await _controller.GetMarket(null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsAssignableFrom<IEnumerable<MarketListingDTO>>(ok.Value);
        Assert.Single(returned);
    }

    [Fact]
    public async Task GetMarket_PassesQueryParametersToService()
    {
        var listings = new List<MarketListingDTO>();
        _marketServiceMock
            .Setup(s => s.GetListingsAsync(ItemCategory.Armor, 2, 10, "desc"))
            .ReturnsAsync(listings);

        var result = await _controller.GetMarket(ItemCategory.Armor, 2, 10, "desc");

        _marketServiceMock.Verify(s => s.GetListingsAsync(ItemCategory.Armor, 2, 10, "desc"), Times.Once);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreateListing_ReturnsCreatedListing()
    {
        var dto = new CreateMarketListingDTO { ItemId = Guid.NewGuid(), Price = 200 };
        var returnedListing = new MarketListing
        {
            Id = Guid.NewGuid(),
            ItemId = dto.ItemId,
            Price = 200,
            SellerId = _userId,
            Category = ItemCategory.Armor
        };

        _marketServiceMock
            .Setup(s => s.CreateListingAsync(It.IsAny<Guid>(), It.IsAny<CreateMarketListingDTO>()))
            .ReturnsAsync(returnedListing);

        var result = await _controller.CreateListing(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var listing = Assert.IsType<MarketListing>(ok.Value);
        Assert.Equal(dto.ItemId, listing.ItemId);
        Assert.Equal(200, listing.Price);

        _marketServiceMock.Verify(s => s.CreateListingAsync(_userId, dto), Times.Once);
    }

    [Fact]
    public async Task Buy_ReturnsOk_WhenPurchaseSuccessful()
    {
        var itemId = Guid.NewGuid();
        _marketServiceMock
            .Setup(s => s.BuyItemAsync(_userId, itemId))
            .Returns(Task.CompletedTask);

        var result = await _controller.Buy(itemId);

        Assert.IsType<OkObjectResult>(result);
        _marketServiceMock.Verify(s => s.BuyItemAsync(_userId, itemId), Times.Once);
    }

    [Fact]
    public async Task Buy_Throws_WhenServiceFails()
    {
        var itemId = Guid.NewGuid();
        _marketServiceMock
            .Setup(s => s.BuyItemAsync(_userId, itemId))
            .ThrowsAsync(new InvalidOperationException("Not enough currency"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Buy(itemId));
    }
}