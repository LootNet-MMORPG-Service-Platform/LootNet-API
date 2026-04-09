using LootNet_API.DTO;
using LootNet_API.Enums;
using LootNet_API.Extensions;
using LootNet_API.Models;
using LootNet_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/market")]
[Authorize]
public class MarketplaceController : Controller
{
    private readonly IMarketplaceService _marketplaceService;

    public MarketplaceController(IMarketplaceService marketplaceService)
    {
        _marketplaceService = marketplaceService;
    }

    [HttpGet("listing")]
    public async Task<IActionResult> GetMarket(
        [FromQuery] ItemCategory? category,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "asc")
    {
        var listings = await _marketplaceService.GetListingsAsync(category, pageNumber, pageSize, sort);
        return Ok(listings);
    }

    [HttpPost("sell")]
    public async Task<IActionResult> CreateListing([FromBody] CreateMarketListingDTO dto)
    {
        var userId = User.GetUserId();
        var listing = await _marketplaceService.CreateListingAsync(userId, dto);
        return Ok(listing);
    }

    [HttpPost("{id}/buy")]
    public async Task<IActionResult> Buy(Guid id)
    {
        var userId = User.GetUserId();
        await _marketplaceService.BuyItemAsync(userId, id);
        return Ok(new { message = "Item bought!" });
    }
}