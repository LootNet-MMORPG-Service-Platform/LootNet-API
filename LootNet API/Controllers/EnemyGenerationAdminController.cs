using LootNet_API.DTO.EnemyGeneration.Create;
using LootNet_API.DTO.EnemyGeneration.Update;
using LootNet_API.Extensions;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LootNet_API.Controllers;

[ApiController]
[Route("api/admin/enemy-generation")]
[Authorize(Roles = "SuperAdmin,GameModerator")]
public class EnemyGenerationAdminController : ControllerBase
{
    private readonly IEnemyGenerationAdminService _service;

    public EnemyGenerationAdminController(IEnemyGenerationAdminService service)
    {
        _service = service;
    }

    [HttpPost("profiles")]
    public async Task<IActionResult> CreateStageProfile(CreateStageProfileDTO dto)
        => Ok(await _service.CreateStageProfileAsync(dto, User.GetUserId()));

    [HttpGet("profiles")]
    public async Task<IActionResult> GetStageProfiles()
        => Ok(await _service.GetStageProfilesAsync());

    [HttpPut("profiles")]
    public async Task<IActionResult> UpdateStageProfile(UpdateStageProfileDTO dto)
    {
        await _service.UpdateStageProfileAsync(dto, User.GetUserId());
        return Ok();
    }

    [HttpDelete("profiles/{id}")]
    public async Task<IActionResult> DeleteStageProfile(Guid id)
    {
        await _service.DeleteStageProfileAsync(id, User.GetUserId());
        return Ok();
    }

    [HttpPost("profiles/{stageProfileId}/scenarios")]
    public async Task<IActionResult> CreateStageScenario(Guid stageProfileId, CreateStageScenarioDTO dto)
        => Ok(await _service.CreateStageScenarioAsync(stageProfileId, dto, User.GetUserId()));

    [HttpGet("profiles/{stageProfileId}/scenarios")]
    public async Task<IActionResult> GetStageScenarios(Guid stageProfileId)
        => Ok(await _service.GetStageScenariosAsync(stageProfileId));

    [HttpPut("scenarios")]
    public async Task<IActionResult> UpdateStageScenario(UpdateStageScenarioDTO dto)
    {
        await _service.UpdateStageScenarioAsync(dto, User.GetUserId());
        return Ok();
    }

    [HttpDelete("scenarios/{id}")]
    public async Task<IActionResult> DeleteStageScenario(Guid id)
    {
        await _service.DeleteStageScenarioAsync(id, User.GetUserId());
        return Ok();
    }

    [HttpPost("scenarios/{scenarioId}/slots")]
    public async Task<IActionResult> CreateScenarioSlot(Guid scenarioId, CreateScenarioSlotDTO dto)
        => Ok(await _service.CreateScenarioSlotAsync(scenarioId, dto, User.GetUserId()));

    [HttpGet("scenarios/{scenarioId}/slots")]
    public async Task<IActionResult> GetScenarioSlots(Guid scenarioId)
        => Ok(await _service.GetScenarioSlotsAsync(scenarioId));

    [HttpPut("slots")]
    public async Task<IActionResult> UpdateScenarioSlot(UpdateScenarioSlotDTO dto)
    {
        await _service.UpdateScenarioSlotAsync(dto, User.GetUserId());
        return Ok();
    }

    [HttpDelete("slots/{id}")]
    public async Task<IActionResult> DeleteScenarioSlot(Guid id)
    {
        await _service.DeleteScenarioSlotAsync(id, User.GetUserId());
        return Ok();
    }

    [HttpPost("class-profiles")]
    public async Task<IActionResult> CreateEnemyClassProfile(CreateEnemyClassProfileDTO dto)
        => Ok(await _service.CreateEnemyClassProfileAsync(dto, User.GetUserId()));

    [HttpGet("class-profiles")]
    public async Task<IActionResult> GetEnemyClassProfiles()
        => Ok(await _service.GetEnemyClassProfilesAsync());

    [HttpPut("class-profiles")]
    public async Task<IActionResult> UpdateEnemyClassProfile(UpdateEnemyClassProfileDTO dto)
    {
        await _service.UpdateEnemyClassProfileAsync(dto, User.GetUserId());
        return Ok();
    }

    [HttpDelete("class-profiles/{id}")]
    public async Task<IActionResult> DeleteEnemyClassProfile(Guid id)
    {
        await _service.DeleteEnemyClassProfileAsync(id, User.GetUserId());
        return Ok();
    }
}
