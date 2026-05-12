namespace LootNet_API.Controllers;
using System.Threading.Tasks;
using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.DTO.Items;
using LootNet_API.Enums;
using LootNet_API.Extensions;
using LootNet_API.Models.GameRun;
using LootNet_API.Models.Items;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/mobile")]
[Authorize]
public class MobileController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IItemGenerationService _itemGenerationService;
    private readonly IInventoryService _inventoryService;
    private readonly IEquipmentService _equipmentService;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IProfileService _profileService;

    public MobileController(AppDbContext context,
        IItemGenerationService itemGenerationService, IInventoryService inventoryService, IEquipmentService equipmentService,
        IRealtimeNotifier realtimeNotifier, IProfileService profileService)
    {
        _context = context;
        _itemGenerationService = itemGenerationService;
        _inventoryService = inventoryService;
        _equipmentService = equipmentService;
        _realtimeNotifier = realtimeNotifier;
        _profileService = profileService;
    }

    [HttpGet("me")]
    public IActionResult GetProfile()
    {
        var userId = User.GetUserId();

        var user = _context.Users.FirstOrDefault(x => x.Id == userId);

        if (user == null)
            return NotFound();

        return Ok(new UserProfileDTO
        {
            Username = user.Username,
            Currency = user.Currency,
            Role = user.Role,
            ProfileImagePath = user.ProfileImagePath
        });
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

    [HttpPost("daily")]
    public async Task<IActionResult> Daily()
    {
        var userId = User.GetUserId();
        var user = _context.Users.FirstOrDefault(x => x.Id == userId);
        if (user == null) return NotFound();

        if (user.LastDailyReward.HasValue &&
            user.LastDailyReward.Value.Date == DateTime.UtcNow.Date)
        {
            return BadRequest("Daily already claimed");
        }

        Item item;
        try
        {
            item = await _itemGenerationService.GenerateItemAsync(userId);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }

        switch (item)
        {
            case Weapon w:
                _context.Weapons.Add(w);
                break;
            case Armor a:
                _context.Armors.Add(a);
                break;
        }

        _context.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemId = item.Id
        });

        user.LastDailyReward = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _realtimeNotifier.AppChangedAsync("reward", "daily-claimed", userId, new { itemId = item.Id, item.Name });

        return Ok(new ItemRewardDTO
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
        });
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetItems()
    {
        var userId = User.GetUserId();
        return Ok(await _inventoryService.GetItemsAsync(userId));
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory()
    {
        var userId = User.GetUserId();
        return Ok(await _inventoryService.GetInventoryAsync(userId));
    }

    [HttpGet("inventory/run")]
    public async Task<IActionResult> GetRunInventory()
    {
        var userId = User.GetUserId();
        return Ok(await _inventoryService.GetRunInventoryAsync(userId));
    }

    [HttpGet("inventory/market")]
    public async Task<IActionResult> GetMarketInventory()
    {
        var userId = User.GetUserId();
        return Ok(await _inventoryService.GetMarketInventoryAsync(userId));
    }

    [HttpPost("inventory/market/return")]
    public async Task<IActionResult> ReturnFromMarket([FromBody] ReturnFromMarketDTO dto)
    {
        try
        {
            var userId = User.GetUserId();
            await _inventoryService.ReturnFromMarketAsync(userId, dto.ItemId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = string.IsNullOrWhiteSpace(ex.Message) ? "Item not found in market inventory." : ex.Message });
        }
    }

    [HttpGet("equipment")]
    public async Task<IActionResult> GetEquipment()
    {
        var userId = User.GetUserId();
        return Ok(await _equipmentService.GetEquipmentAsync(userId));
    }

    [HttpPost("equip/weapon/{slot}/{itemId}")]
    public async Task<IActionResult> EquipWeapon(int slot, Guid itemId)
    {
        var userId = User.GetUserId();

        var run = await GetActiveRun(userId);

        if (run != null)
        {
            if (run.Status == RunStatus.InBattle)
                return BadRequest("Cannot change equipment during battle");

            await _equipmentService.EquipWeaponFromRunAsync(userId, itemId, slot);
        }
        else
        {
            await _equipmentService.EquipWeaponAsync(userId, itemId, slot);
        }
        await _realtimeNotifier.AppChangedAsync("equipment", "equip-weapon-mobile", userId, new { itemId, slot });

        return Ok();
    }

    [HttpPost("equip/armor/{itemId}")]
    public async Task<IActionResult> EquipArmor(Guid itemId)
    {
        var userId = User.GetUserId();

        var run = await GetActiveRun(userId);

        if (run != null)
        {
            if (run.Status == RunStatus.InBattle)
                return BadRequest("Cannot change equipment during battle");

            await _equipmentService.EquipArmorFromRunAsync(userId, itemId);
        }
        else
        {
            await _equipmentService.EquipArmorAsync(userId, itemId);
        }
        await _realtimeNotifier.AppChangedAsync("equipment", "equip-armor-mobile", userId, new { itemId });

        return Ok();
    }

    [HttpPost("unequip/{itemId}")]
    public async Task<IActionResult> Unequip(Guid itemId)
    {
        var userId = User.GetUserId();

        var run = await GetActiveRun(userId);

        if (run != null && run.Status == RunStatus.InBattle)
            return BadRequest("Cannot unequip during battle");

        await _equipmentService.UnequipItemAsync(userId, itemId);
        await _realtimeNotifier.AppChangedAsync("equipment", "unequip-mobile", userId, new { itemId });

        return Ok();
    }
    private async Task<Run?> GetActiveRun(Guid userId)
    {
        return await _context.Runs
            .Include(x => x.Battles)
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                (x.Status == RunStatus.Active || x.Status == RunStatus.InBattle));
    }
}
