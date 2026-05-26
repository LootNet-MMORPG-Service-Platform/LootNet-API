using LootNet_API.DTO;
using LootNet_API.DTO.Generation.Create;
using LootNet_API.DTO.Generation.Update;
using LootNet_API.Enums;
using LootNet_API.Extensions;
using LootNet_API.Services;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LootNet_API.Controllers;

[ApiController]
[Route("api/admin/generation")]
[Authorize(Roles = "SuperAdmin,GameModerator")]
public class GenerationAdminController : ControllerBase
{
    private readonly IGenerationAdminService _service;

    public GenerationAdminController(IGenerationAdminService service)
    {
        _service = service;
    }

    #region CREATE

    [HttpPost("profiles")]
    public async Task<IActionResult> CreateProfile(CreateGenerationProfileDTO dto)
        => Ok(await _service.CreateProfileAsync(dto, User.GetUserId()));

    [HttpPost("profiles/full")]
    public async Task<IActionResult> CreateProfileFull(CreateGenerationProfileFullDTO dto)
        => Ok(await _service.CreateProfileAsync(dto, User.GetUserId()));

    [HttpPost("profiles/{profileId}/rules")]
    public async Task<IActionResult> CreateRule(Guid profileId, CreateRuleDTO dto)
        => Ok(await _service.CreateRuleAsync(profileId, dto, User.GetUserId()));

    [HttpPost("rules/full")]
    public async Task<IActionResult> CreateFullRule(CreateRuleFullDTO dto)
        => Ok(await _service.CreateFullRuleAsync(dto, User.GetUserId()));

    [HttpPost("rules/{ruleId}/parameters")]
    public async Task<IActionResult> CreateParameter(Guid ruleId, CreateParameterDTO dto)
        => Ok(await _service.CreateParameterAsync(ruleId, dto, User.GetUserId()));

    [HttpPost("rules/{ruleId}/elements")]
    public async Task<IActionResult> CreateElement(Guid ruleId, CreateElementDTO dto)
        => Ok(await _service.CreateElementAsync(ruleId, dto, User.GetUserId()));

    [HttpPost("profiles/{profileId}/weights")]
    public async Task<IActionResult> CreateWeight(Guid profileId, CreateTypeWeightDTO dto)
        => Ok(await _service.CreateWeightAsync(profileId, dto, User.GetUserId()));

    #endregion

    #region READ

    [HttpGet("profiles")]
    public async Task<IActionResult> GetProfiles()
        => Ok(await _service.GetProfilesAsync());

    [HttpGet("profiles/{id}")]
    public async Task<IActionResult> GetProfile(Guid id)
        => Ok(await _service.GetProfileDetailsAsync(id));

    [HttpGet("profiles/{profileId}/rules")]
    public async Task<IActionResult> GetRules(Guid profileId)
        => Ok(await _service.GetRulesAsync(profileId));

    [HttpGet("rules/{ruleId}/parameters")]
    public async Task<IActionResult> GetParameters(Guid ruleId)
        => Ok(await _service.GetParametersAsync(ruleId));

    [HttpGet("rules/{ruleId}/elements")]
    public async Task<IActionResult> GetElements(Guid ruleId)
        => Ok(await _service.GetElementsAsync(ruleId));

    [HttpGet("profiles/{profileId}/weights")]
    public async Task<IActionResult> GetWeights(Guid profileId)
        => Ok(await _service.GetWeightsAsync(profileId));

    #endregion

    #region UPDATE

    [HttpPut("profiles")]
    public async Task<IActionResult> UpdateProfile(UpdateGenerationProfileDTO dto)
    {
        await _service.UpdateProfileAsync(dto, User.GetUserId());
        return Ok();
    }

    [HttpPut("rules")]
    public async Task<IActionResult> UpdateRule(UpdateRuleDTO dto)
    {
        await _service.UpdateRuleAsync(dto, User.GetUserId());
        return Ok();
    }

    [HttpPut("parameters")]
    public async Task<IActionResult> UpdateParameter(UpdateParameterDTO dto)
    {
        await _service.UpdateParameterAsync(dto, User.GetUserId());
        return Ok();
    }

    [HttpPut("elements")]
    public async Task<IActionResult> UpdateElement(UpdateElementDTO dto)
    {
        await _service.UpdateElementAsync(dto, User.GetUserId());
        return Ok();
    }

    [HttpPut("weights")]
    public async Task<IActionResult> UpdateWeight(UpdateTypeWeightDTO dto)
    {
        await _service.UpdateWeightAsync(dto, User.GetUserId());
        return Ok();
    }

    #endregion

    #region DELETE

    [HttpDelete("profiles/{id}")]
    public async Task<IActionResult> DeleteProfile(Guid id)
    {
        await _service.DeleteProfileAsync(id, User.GetUserId());
        return Ok();
    }

    [HttpDelete("rules/{id}")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        await _service.DeleteRuleAsync(id, User.GetUserId());
        return Ok();
    }

    [HttpDelete("parameters/{id}")]
    public async Task<IActionResult> DeleteParameter(Guid id)
    {
        await _service.DeleteParameterAsync(id, User.GetUserId());
        return Ok();
    }

    [HttpDelete("elements/{id}")]
    public async Task<IActionResult> DeleteElement(Guid id)
    {
        await _service.DeleteElementAsync(id, User.GetUserId());
        return Ok();
    }

    [HttpDelete("weights/{id}")]
    public async Task<IActionResult> DeleteWeight(Guid id)
    {
        await _service.DeleteWeightAsync(id, User.GetUserId());
        return Ok();
    }

    #endregion
}
