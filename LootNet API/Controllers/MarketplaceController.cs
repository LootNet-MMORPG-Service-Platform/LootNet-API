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
        var result = await _marketplaceService.GetWeaponsAsync(query);
        return Ok(result);
    }

    [HttpPost("listing/armors")]
    public async Task<IActionResult> GetArmors([FromBody] ArmorQueryDTO query)
    {
        var result = await _marketplaceService.GetArmorsAsync(query);
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
        var userId = User.GetUserId();

        await _marketplaceService.BuyItemAsync(userId, id);

        return Ok();
    }
}