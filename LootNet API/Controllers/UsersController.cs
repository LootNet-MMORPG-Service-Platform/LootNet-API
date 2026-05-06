using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LootNet_API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMarketplaceService _marketplaceService;

    public UsersController(AppDbContext context, IMarketplaceService marketplaceService)
    {
        _context = context;
        _marketplaceService = marketplaceService;
    }

    [HttpGet("{id}")]
    public IActionResult GetUser(Guid id)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == id);
        if (user == null) return NotFound();
        return Ok(new { userId = user.Id, username = user.Username, profileImagePath = user.ProfileImagePath });
    }

    [HttpPost("{id}/listings")]
    public async Task<IActionResult> GetUserListings(Guid id, [FromBody] MyListingsQueryDTO query)
    {
        var result = await _marketplaceService.GetListingsBySellerAsync(id, query);
        return Ok(result);
    }
}
