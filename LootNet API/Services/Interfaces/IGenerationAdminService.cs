namespace LootNet_API.Services.Interfaces;

using Models.Items;
using DTO.Generation;
using LootNet_API.DTO.Generation.Create;
using LootNet_API.DTO.Generation.Update;
using LootNet_API.DTO.Generation.Response;

public interface IGenerationAdminService
{
    Task<Guid> CreateProfileAsync(CreateGenerationProfileDTO dto, Guid adminId = default);
    Task<Guid> CreateProfileAsync(CreateGenerationProfileFullDTO dto, Guid adminId = default);

    Task<List<GenerationProfileDTO>> GetProfilesAsync();
    Task<GenerationProfileDetailsDTO> GetProfileDetailsAsync(Guid id);

    Task UpdateProfileAsync(UpdateGenerationProfileDTO dto, Guid adminId = default);
    Task DeleteProfileAsync(Guid id, Guid adminId = default);

    Task<Guid> CreateRuleAsync(Guid profileId, CreateRuleDTO dto, Guid adminId = default);
    Task<Guid> CreateFullRuleAsync(CreateRuleFullDTO dto, Guid adminId = default);

    Task<List<RuleDTO>> GetRulesAsync(Guid profileId);

    Task UpdateRuleAsync(UpdateRuleDTO dto, Guid adminId = default);
    Task DeleteRuleAsync(Guid id, Guid adminId = default);

    Task<Guid> CreateParameterAsync(Guid ruleId, CreateParameterDTO dto, Guid adminId = default);
    Task<List<ParameterDTO>> GetParametersAsync(Guid ruleId);
    Task UpdateParameterAsync(UpdateParameterDTO dto, Guid adminId = default);
    Task DeleteParameterAsync(Guid id, Guid adminId = default);

    Task<Guid> CreateElementAsync(Guid ruleId, CreateElementDTO dto, Guid adminId = default);
    Task<List<ElementDTO>> GetElementsAsync(Guid ruleId);
    Task UpdateElementAsync(UpdateElementDTO dto, Guid adminId = default);
    Task DeleteElementAsync(Guid id, Guid adminId = default);

    Task<Guid> CreateWeightAsync(Guid profileId, CreateTypeWeightDTO dto, Guid adminId = default);
    Task<List<TypeWeightDTO>> GetWeightsAsync(Guid profileId);
    Task UpdateWeightAsync(UpdateTypeWeightDTO dto, Guid adminId = default);
    Task DeleteWeightAsync(Guid id, Guid adminId = default);
}
