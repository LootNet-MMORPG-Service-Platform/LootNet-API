using LootNet_API.DTO.GameRun;
using LootNet_API.Extensions;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LootNet_API.Controllers;

[ApiController]
[Route("api/run")]
public class GameRunController : ControllerBase
{
    private readonly IGameRunService _service;

    public GameRunController(IGameRunService service)
    {
        _service = service;
    }

    [HttpGet("active")]
    public async Task<IActionResult> Active()
    {
        var run = await _service.GetActiveRunAsync(User.GetUserId());
        return Ok(run);
    }

    [HttpGet("battle/current")]
    public async Task<IActionResult> CurrentBattle()
    {
        var battle = await _service.GetCurrentBattleAsync(User.GetUserId());
        return Ok(battle);
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start(StartRunDTO dto)
    {
        if (dto.ItemIds == null || dto.ItemIds.Count == 0)
            return BadRequest("At least one item is required to start run.");

        try
        {
            return Ok(await _service.StartRunAsync(User.GetUserId(), dto));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("go-further")]
    public async Task<IActionResult> GoFurther()
    {
        try
        {
            return Ok(await _service.GoFurtherAsync(User.GetUserId()));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("turn")]
    public async Task<IActionResult> FinishTurn(FinishTurnDTO dto)
    {
        try
        {
            return Ok(await _service.FinishTurnAsync(User.GetUserId(), dto));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("end")]
    public async Task<IActionResult> End()
    {
        try
        {
            await _service.EndRunAsync(User.GetUserId());
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("force-return/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ForceReturn(Guid userId)
    {
        try
        {
            return Ok(await _service.ForceReturnAsync(userId));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
