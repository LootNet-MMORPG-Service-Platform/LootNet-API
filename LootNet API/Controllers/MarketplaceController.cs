using LootNet_API.DTO;
using LootNet_API.Enums;
using LootNet_API.Extensions;
using LootNet_API.Models;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/market")]
[Authorize]
public class MarketplaceController : Controller
{
    private readonly IMarketplaceService _marketplaceService;
    private readonly IInventoryService _inventoryService;

    public MarketplaceController(
        IMarketplaceService marketplaceService, IInventoryService inventoryService)
    {
        _marketplaceService = marketplaceService;
        _inventoryService = inventoryService;
    }

    [HttpPost("listing/weapons")]
    public async Task<IActionResult> GetWeapons([FromBody] WeaponQueryDTO query)
    {
        var userId = User.GetUserId();
        var result = await _marketplaceService.GetWeaponsAsync(userId, query);
        return Ok(result);
    }

    [HttpPost("listing/armors")]
    public async Task<IActionResult> GetArmors([FromBody] ArmorQueryDTO query)
    {
        var userId = User.GetUserId();
        var result = await _marketplaceService.GetArmorsAsync(userId, query);
        return Ok(result);
    }

    [HttpGet("me/listings")]
    public async Task<IActionResult> GetMyListings()
    {
        var userId = User.GetUserId();
        var result = await _marketplaceService.GetMyListingsAsync(userId);
        return Ok(result);
    }

    [HttpGet("me/transactions")]
    public async Task<IActionResult> GetMyTransactions()
    {
        var userId = User.GetUserId();
        var result = await _marketplaceService.GetMyTransactionsAsync(userId);
        return Ok(result);
    }

    [HttpPost("sell")]
    public async Task<IActionResult> CreateListing(CreateMarketListingDTO dto)
    {
        var userId = User.GetUserId();

        await _inventoryService.MoveToMarketAsync(userId, dto.ItemId);

        var listing = await _marketplaceService.CreateListingAsync(userId, dto);

        return Ok(listing);
    }

    [HttpPost("{id}/buy")]
    public async Task<IActionResult> Buy(Guid id)
    {
        try
        {
            var userId = User.GetUserId();
            await _marketplaceService.BuyItemAsync(userId, id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
