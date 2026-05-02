using LootNet_API.DTO.GameRun;
using LootNet_API.Extensions;
using LootNet_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LootNet_API.Controllers;

[ApiController]
[Route("api/run")]
public class GameRunController : ControllerBase
{
    private readonly GameRunService _service;

    public GameRunController(GameRunService service)
    {
        _service = service;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start(StartRunDTO dto)
    {
        return Ok(await _service.StartRunAsync(User.GetUserId(), dto));
    }

    [HttpPost("go-further")]
    public async Task<IActionResult> GoFurther()
    {
        return Ok(await _service.GoFurtherAsync(User.GetUserId()));
    }

    [HttpPost("turn")]
    public async Task<IActionResult> FinishTurn(FinishTurnDTO dto)
    {
        return Ok(await _service.FinishTurnAsync(User.GetUserId(), dto));
    }

    [HttpPost("end")]
    public async Task<IActionResult> End()
    {
        await _service.EndRunAsync(User.GetUserId());
        return Ok();
    }

    [HttpPost("force-return/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ForceReturn(Guid userId)
    {
        return Ok(await _service.ForceReturnAsync(userId));
    }
}