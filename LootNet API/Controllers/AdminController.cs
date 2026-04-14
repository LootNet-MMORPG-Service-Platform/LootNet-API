using LootNet_API.DTO.Admin;
using LootNet_API.Extensions;
using LootNet_API.Models;
using LootNet_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LootNet_API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _service;

    public AdminController(AdminService service)
    {
        _service = service;
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        return Ok(await _service.GetUserAsync(id));
    }

    [HttpGet("users/{id}/inventory")]
    public async Task<IActionResult> GetInventory(Guid id)
    {
        return Ok(await _service.GetUserInventoryAsync(id));
    }

    [HttpGet("users/{id}/equipment")]
    public async Task<IActionResult> GetEquipment(Guid id)
    {
        return Ok(await _service.GetUserEquipmentAsync(id));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersQueryDTO query)
    {
        return Ok(await _service.GetUsersAsync(query));
    }

    [HttpPost("users/{id}/block")]
    public async Task<IActionResult> Block(Guid id, BlockUserDTO dto)
    {
        var adminId = User.GetUserId();

        await _service.BlockUserAsync(adminId, id, dto.Reason, dto.Days);
        return Ok();
    }

    [HttpPost("users/{id}/unblock")]
    public async Task<IActionResult> Unblock(Guid id)
    {
        var adminId = User.GetUserId();

        await _service.UnblockUserAsync(adminId, id);
        return Ok();
    }

    [HttpPost("users/{id}/role")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ChangeRole(Guid id, ChangeRoleDTO dto)
    {
        var adminId = User.GetUserId();

        await _service.ChangeRoleAsync(adminId, id, dto.Role);
        return Ok();
    }
}
