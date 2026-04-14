namespace LootNet_API.Services.Interfaces;

using Models.Items;
using DTO.Generation;
using LootNet_API.DTO.Generation.Create;
using LootNet_API.DTO.Generation.Update;
using LootNet_API.DTO.Generation.Response;

public interface IGenerationAdminService
{
    Task<Guid> CreateProfileAsync(CreateGenerationProfileDTO dto);
    Task<Guid> CreateProfileAsync(CreateGenerationProfileFullDTO dto);

    Task<List<GenerationProfileDTO>> GetProfilesAsync();
    Task<GenerationProfileDetailsDTO> GetProfileDetailsAsync(Guid id);

    Task UpdateProfileAsync(UpdateGenerationProfileDTO dto);
    Task DeleteProfileAsync(Guid id);

    Task<Guid> CreateRuleAsync(Guid profileId, CreateRuleDTO dto);
    Task<Guid> CreateFullRuleAsync(CreateRuleFullDTO dto);

    Task<List<RuleDTO>> GetRulesAsync(Guid profileId);

    Task UpdateRuleAsync(UpdateRuleDTO dto);
    Task DeleteRuleAsync(Guid id);

    Task<Guid> CreateParameterAsync(Guid ruleId, CreateParameterDTO dto);
    Task<List<ParameterDTO>> GetParametersAsync(Guid ruleId);
    Task UpdateParameterAsync(UpdateParameterDTO dto);
    Task DeleteParameterAsync(Guid id);

    Task<Guid> CreateElementAsync(Guid ruleId, CreateElementDTO dto);
    Task<List<ElementDTO>> GetElementsAsync(Guid ruleId);
    Task UpdateElementAsync(UpdateElementDTO dto);
    Task DeleteElementAsync(Guid id);

    Task<Guid> CreateWeightAsync(Guid profileId, CreateTypeWeightDTO dto);
    Task<List<TypeWeightDTO>> GetWeightsAsync(Guid profileId);
    Task UpdateWeightAsync(UpdateTypeWeightDTO dto);
    Task DeleteWeightAsync(Guid id);
}