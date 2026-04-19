namespace LootNet_API.Controllers;
using LootNet_API.Data;
using LootNet_API.DTO.Items;
using LootNet_API.DTO;
using LootNet_API.Enums;
using LootNet_API.Extensions;
using LootNet_API.Hubs;
using LootNet_API.Models.Items;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using LootNet_API.Services.Interfaces;
using System.Threading.Tasks;

[ApiController]
[Route("api/mobile")]
[Authorize]
public class MobileController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<GameHub> _hub;
    private readonly IItemGenerationService _itemGenerationService;
    private readonly IInventoryService _inventoryService;

    public MobileController(AppDbContext context, IHubContext<GameHub> hub,
        IItemGenerationService itemGenerationService, IInventoryService inventoryService)
    {
        _context = context;
        _hub = hub;
        _itemGenerationService = itemGenerationService;
        _inventoryService = inventoryService;
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
            Role = user.Role
        });
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

        user.LastDailyReward = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _hub.Clients.User(userId.ToString())
            .SendAsync("ItemGenerated", item.Name);

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

    [HttpGet("equipment")]
    public async Task<IActionResult> GetEquipment()
    {
        var userId = User.GetUserId();
        return Ok(await _inventoryService.GetEquipmentAsync(userId));
    }

    [HttpPost("equip/weapon/{slot}/{itemId}")]
    public async Task<IActionResult> EquipWeapon(int slot, Guid itemId)
    {
        var userId = User.GetUserId();
        await _inventoryService.EquipWeaponAsync(userId, itemId, slot);
        return Ok();
    }

    [HttpPost("equip/armor/{itemId}")]
    public async Task<IActionResult> EquipArmor(Guid itemId)
    {
        var userId = User.GetUserId();
        await _inventoryService.EquipArmorAsync(userId, itemId);
        return Ok();
    }

    [HttpPost("unequip/{itemId}")]
    public async Task<IActionResult> Unequip(Guid itemId)
    {
        var userId = User.GetUserId();
        await _inventoryService.UnequipItemAsync(userId, itemId);
        return Ok();
    }
}
