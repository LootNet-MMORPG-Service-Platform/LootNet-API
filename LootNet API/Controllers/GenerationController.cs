using LootNet_API.DTO;
using LootNet_API.DTO.Generation.Create;
using LootNet_API.DTO.Generation.Update;
using LootNet_API.Enums;
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
        => Ok(await _service.CreateProfileAsync(dto));

    [HttpPost("profiles/full")]
    public async Task<IActionResult> CreateProfileFull(CreateGenerationProfileFullDTO dto)
        => Ok(await _service.CreateProfileAsync(dto));

    [HttpPost("profiles/{profileId}/rules")]
    public async Task<IActionResult> CreateRule(Guid profileId, CreateRuleDTO dto)
        => Ok(await _service.CreateRuleAsync(profileId, dto));

    [HttpPost("rules/full")]
    public async Task<IActionResult> CreateFullRule(CreateRuleFullDTO dto)
        => Ok(await _service.CreateFullRuleAsync(dto));

    [HttpPost("rules/{ruleId}/parameters")]
    public async Task<IActionResult> CreateParameter(Guid ruleId, CreateParameterDTO dto)
        => Ok(await _service.CreateParameterAsync(ruleId, dto));

    [HttpPost("rules/{ruleId}/elements")]
    public async Task<IActionResult> CreateElement(Guid ruleId, CreateElementDTO dto)
        => Ok(await _service.CreateElementAsync(ruleId, dto));

    [HttpPost("profiles/{profileId}/weights")]
    public async Task<IActionResult> CreateWeight(Guid profileId, CreateTypeWeightDTO dto)
        => Ok(await _service.CreateWeightAsync(profileId, dto));

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
        await _service.UpdateProfileAsync(dto);
        return Ok();
    }

    [HttpPut("rules")]
    public async Task<IActionResult> UpdateRule(UpdateRuleDTO dto)
    {
        await _service.UpdateRuleAsync(dto);
        return Ok();
    }

    [HttpPut("parameters")]
    public async Task<IActionResult> UpdateParameter(UpdateParameterDTO dto)
    {
        await _service.UpdateParameterAsync(dto);
        return Ok();
    }

    [HttpPut("elements")]
    public async Task<IActionResult> UpdateElement(UpdateElementDTO dto)
    {
        await _service.UpdateElementAsync(dto);
        return Ok();
    }

    [HttpPut("weights")]
    public async Task<IActionResult> UpdateWeight(UpdateTypeWeightDTO dto)
    {
        await _service.UpdateWeightAsync(dto);
        return Ok();
    }

    #endregion

    #region DELETE

    [HttpDelete("profiles/{id}")]
    public async Task<IActionResult> DeleteProfile(Guid id)
    {
        await _service.DeleteProfileAsync(id);
        return Ok();
    }

    [HttpDelete("rules/{id}")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        await _service.DeleteRuleAsync(id);
        return Ok();
    }

    [HttpDelete("parameters/{id}")]
    public async Task<IActionResult> DeleteParameter(Guid id)
    {
        await _service.DeleteParameterAsync(id);
        return Ok();
    }

    [HttpDelete("elements/{id}")]
    public async Task<IActionResult> DeleteElement(Guid id)
    {
        await _service.DeleteElementAsync(id);
        return Ok();
    }

    [HttpDelete("weights/{id}")]
    public async Task<IActionResult> DeleteWeight(Guid id)
    {
        await _service.DeleteWeightAsync(id);
        return Ok();
    }

    #endregion
}