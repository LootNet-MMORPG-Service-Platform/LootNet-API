namespace LootNet_API.Controllers;
using LootNet_API.Data;
using LootNet_API.Enums;
using LootNet_API.Extensions;
using LootNet_API.Hubs;
using LootNet_API.Models.Items;
using LootNet_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

[ApiController]
[Route("api/mobile")]
[Authorize]
public class MobileController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<GameHub> _hub;
    private readonly IItemGenerationService _itemGenerationService;

    public MobileController(AppDbContext context, IHubContext<GameHub> hub, IItemGenerationService itemGenerationService)
    {
        _context = context;
        _hub = hub;
        _itemGenerationService = itemGenerationService;
    }

    [HttpGet("me")]
    public IActionResult GetProfile()
    {
        var userId = User.GetUserId();

        var user = _context.Users.FirstOrDefault(x => x.Id == userId);

        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.Username,
            user.Currency,
            user.Role
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

        return Ok(item);
    }

    [HttpGet("items")]
    public IActionResult GetItems()
    {
        var userId = User.GetUserId();

        var weapons = _context.Weapons
            .Where(x => x.OwnerId == userId)
            .ToList<Item>();

        var armors = _context.Armors
            .Where(x => x.OwnerId == userId)
            .ToList<Item>();

        var allItems = weapons.Concat(armors).ToList();

        return Ok(allItems);
    }
}
