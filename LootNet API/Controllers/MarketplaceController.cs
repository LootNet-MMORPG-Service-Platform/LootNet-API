using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.DTO.Market;
using LootNet_API.Enums;
using LootNet_API.Extensions;
using LootNet_API.Models;
using LootNet_API.Services;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/market")]
[Authorize]
public class MarketplaceController : Controller
{
    private readonly IMarketplaceService _marketplaceService;
    private readonly IProfileService _profileService;

    public MarketplaceController(
        IMarketplaceService marketplaceService,
        IProfileService profileService)
    {
        _marketplaceService = marketplaceService;
        _profileService = profileService;
    }

    [HttpPost("me/pfp")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadProfilePicture([FromForm] UploadProfilePictureRequest request)
    {
        try
        {
            var userId = User.GetUserId();
            var profileImagePath = await _profileService.UploadProfilePictureAsync(userId, request.File);

            return Ok(new { profileImagePath });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = User.GetUserId();
            return Ok(await _profileService.GetProfileAsync(userId));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
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

    [HttpPost("me/listings")]
    public async Task<IActionResult> GetMyListings([FromBody] MyListingsQueryDTO query)
    {
        var userId = User.GetUserId();
        var result = await _marketplaceService.GetMyListingsAsync(userId, query);
        return Ok(result);
    }

    [HttpGet("me/listings/summary")]
    public async Task<IActionResult> GetMyListingsSummary()
    {
        var userId = User.GetUserId();
        var result = await _marketplaceService.GetMyListingsSummaryAsync(userId);
        return Ok(result);
    }

    [HttpPost("me/transactions")]
    public async Task<IActionResult> GetMyTransactions([FromBody] MarketTransactionsQueryDTO query)
    {
        var userId = User.GetUserId();
        var result = await _marketplaceService.GetMyTransactionsAsync(userId, query);
        return Ok(result);
    }

    [HttpGet("me/transactions/summary")]
    public async Task<IActionResult> GetMyTransactionsSummary()
    {
        var userId = User.GetUserId();
        var result = await _marketplaceService.GetMyTransactionsSummaryAsync(userId);
        return Ok(result);
    }

    [HttpGet("sell/inventory")]
    public async Task<IActionResult> GetSellInventory([FromQuery] SellInventoryQueryDTO query)
    {
        var userId = User.GetUserId();
        return Ok(await _marketplaceService.GetSellInventoryAsync(userId, query));
    }

    [HttpGet("economy")]
    public IActionResult GetEconomy()
    {
        return Ok(_marketplaceService.GetEconomy());
    }

    [HttpGet("tax-preview")]
    public IActionResult GetTaxPreview([FromQuery] decimal price)
    {
        try
        {
            return Ok(_marketplaceService.CalculateSaleTax(price));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("bot/quote")]
    public async Task<IActionResult> GetBotSaleOffer([FromBody] SellItemToBotDTO dto)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _marketplaceService.GetBotSaleOfferAsync(userId, dto.ItemId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("bot/sell")]
    public async Task<IActionResult> SellItemToBot([FromBody] SellItemToBotDTO dto)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _marketplaceService.SellItemToBotAsync(userId, dto.ItemId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("daily")]
    public async Task<IActionResult> ClaimWebDaily()
    {
        try
        {
            var userId = User.GetUserId();
            return Ok(await _marketplaceService.ClaimWebDailyAsync(userId));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sell")]
    public async Task<IActionResult> CreateListing(CreateMarketListingDTO dto)
    {
        try
        {
            var userId = User.GetUserId();
            var listing = await _marketplaceService.CreateListingAsync(userId, dto);
            return Ok(listing);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

    [HttpPost("{id}/change-price")]
    public async Task<IActionResult> ChangePrice(Guid id, [FromBody] ChangeListingPriceDTO dto)
    {
        try
        {
            var userId = User.GetUserId();
            await _marketplaceService.ChangeListingPriceAsync(userId, id, dto.Price);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var userId = User.GetUserId();
            await _marketplaceService.CancelListingAsync(userId, id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
