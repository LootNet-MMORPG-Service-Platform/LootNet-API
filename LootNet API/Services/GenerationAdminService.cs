using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.DTO.Generation.Create;
using LootNet_API.DTO.Generation.Response;
using LootNet_API.DTO.Generation.Update;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Models.Items.Generation;
using LootNet_API.Models.Logs;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LootNet_API.Services;

public class GenerationAdminService : IGenerationAdminService
{
    private readonly AppDbContext _context;

    public GenerationAdminService(AppDbContext context)
    {
        _context = context;
    }

    #region CREATE

    public async Task<Guid> CreateProfileAsync(CreateGenerationProfileDTO dto)
    {
        var p = new GenerationProfile
        {
            Id = Guid.NewGuid(),
            Name = dto.Name
        };

        _context.GenerationProfiles.Add(p);
        await _context.SaveChangesAsync();
        await LogAsync("CREATE_PROFILE", p.Id);

        return p.Id;
    }

    public async Task<Guid> CreateProfileAsync(CreateGenerationProfileFullDTO dto)
    {
        ValidateProfile(dto);

        var profile = new GenerationProfile
        {
            Id = Guid.NewGuid(),
            Name = dto.Name
        };

        _context.GenerationProfiles.Add(profile);

        foreach (var w in dto.TypeWeights)
        {
            Validate(w.WeaponType, w.ArmorType);

            _context.ItemTypeWeights.Add(new ItemTypeWeight
            {
                Id = Guid.NewGuid(),
                ProfileId = profile.Id,
                Category = w.Category,
                WeaponType = w.WeaponType,
                ArmorType = w.ArmorType,
                Weight = w.Weight
            });
        }

        foreach (var r in dto.Rules)
        {
            Validate(r.WeaponType, r.ArmorType);

            var rule = new ItemGenerationRule
            {
                Id = Guid.NewGuid(),
                ProfileId = profile.Id,
                Category = r.Category,
                WeaponType = r.WeaponType,
                ArmorType = r.ArmorType,
                IsFallback = r.IsFallback
            };

            _context.ItemGenerationRules.Add(rule);
        }

        await _context.SaveChangesAsync();
        await LogAsync("CREATE_PROFILE_FULL", profile.Id);

        return profile.Id;
    }

    public async Task<Guid> CreateRuleAsync(Guid profileId, CreateRuleDTO dto)
    {
        Validate(dto.WeaponType, dto.ArmorType);

        var r = new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            Category = dto.Category,
            WeaponType = dto.WeaponType,
            ArmorType = dto.ArmorType,
            IsFallback = dto.IsFallback
        };

        _context.ItemGenerationRules.Add(r);
        await _context.SaveChangesAsync();

        await LogAsync("CREATE_RULE", r.Id);
        return r.Id;
    }

    public async Task<Guid> CreateFullRuleAsync(CreateRuleFullDTO dto)
    {
        Validate(dto.WeaponType, dto.ArmorType);

        var rule = new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            ProfileId = dto.ProfileId,
            Category = dto.Category,
            WeaponType = dto.WeaponType,
            ArmorType = dto.ArmorType,
            IsFallback = dto.IsFallback
        };

        _context.ItemGenerationRules.Add(rule);

        await _context.SaveChangesAsync();
        await LogAsync("CREATE_RULE_FULL", rule.Id);

        return rule.Id;
    }

    public async Task<Guid> CreateParameterAsync(Guid ruleId, CreateParameterDTO dto)
    {
        var p = new ItemParameterSetting
        {
            Id = Guid.NewGuid(),
            RuleId = ruleId,
            Parameter = dto.Parameter
        };

        _context.ItemParameterSettings.Add(p);
        await _context.SaveChangesAsync();

        await LogAsync("CREATE_PARAMETER", p.Id);
        return p.Id;
    }

    public async Task<Guid> CreateElementAsync(Guid ruleId, CreateElementDTO dto)
    {
        var e = new ItemElementSetting
        {
            Id = Guid.NewGuid(),
            RuleId = ruleId,
            ElementType = dto.ElementType
        };

        _context.ItemElementSettings.Add(e);
        await _context.SaveChangesAsync();

        await LogAsync("CREATE_ELEMENT", e.Id);
        return e.Id;
    }

    public async Task<Guid> CreateWeightAsync(Guid profileId, CreateTypeWeightDTO dto)
    {
        Validate(dto.WeaponType, dto.ArmorType);

        var w = new ItemTypeWeight
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            Category = dto.Category,
            WeaponType = dto.WeaponType,
            ArmorType = dto.ArmorType,
            Weight = dto.Weight
        };

        _context.ItemTypeWeights.Add(w);
        await _context.SaveChangesAsync();

        await LogAsync("CREATE_WEIGHT", w.Id);
        return w.Id;
    }

    #endregion

    #region READ

    public async Task<List<GenerationProfileDTO>> GetProfilesAsync()
        => await _context.GenerationProfiles
            .Select(x => new GenerationProfileDTO { Id = x.Id, Name = x.Name })
            .ToListAsync();

    public async Task<GenerationProfileDetailsDTO> GetProfileDetailsAsync(Guid id)
        => await _context.GenerationProfiles
            .Where(x => x.Id == id)
            .Select(x => new GenerationProfileDetailsDTO
            {
                Id = x.Id,
                Name = x.Name
            })
            .FirstAsync();

    public async Task<List<RuleDTO>> GetRulesAsync(Guid profileId)
        => await _context.ItemGenerationRules
            .Where(x => x.ProfileId == profileId)
            .Select(x => new RuleDTO { Id = x.Id })
            .ToListAsync();

    public async Task<List<ParameterDTO>> GetParametersAsync(Guid ruleId)
        => await _context.ItemParameterSettings
            .Where(x => x.RuleId == ruleId)
            .Select(x => new ParameterDTO { Id = x.Id })
            .ToListAsync();

    public async Task<List<ElementDTO>> GetElementsAsync(Guid ruleId)
        => await _context.ItemElementSettings
            .Where(x => x.RuleId == ruleId)
            .Select(x => new ElementDTO { Id = x.Id })
            .ToListAsync();

    public async Task<List<TypeWeightDTO>> GetWeightsAsync(Guid profileId)
        => await _context.ItemTypeWeights
            .Where(x => x.ProfileId == profileId)
            .Select(x => new TypeWeightDTO { Id = x.Id })
            .ToListAsync();

    #endregion

    #region UPDATE

    public async Task UpdateProfileAsync(UpdateGenerationProfileDTO dto)
    {
        var p = await _context.GenerationProfiles.FirstAsync(x => x.Id == dto.Id);
        p.Name = dto.Name;

        await _context.SaveChangesAsync();
        await LogAsync("UPDATE_PROFILE", p.Id);
    }

    public async Task UpdateRuleAsync(UpdateRuleDTO dto)
    {
        Validate(dto.WeaponType, dto.ArmorType);

        var r = await _context.ItemGenerationRules.FirstAsync(x => x.Id == dto.Id);

        r.Category = dto.Category;
        r.WeaponType = dto.WeaponType;
        r.ArmorType = dto.ArmorType;
        r.IsFallback = dto.IsFallback;

        await _context.SaveChangesAsync();
        await LogAsync("UPDATE_RULE", r.Id);
    }

    public async Task UpdateParameterAsync(UpdateParameterDTO dto)
    {
        var p = await _context.ItemParameterSettings.FirstAsync(x => x.Id == dto.Id);
        p.Parameter = dto.Parameter;

        await _context.SaveChangesAsync();
        await LogAsync("UPDATE_PARAMETER", p.Id);
    }

    public async Task UpdateElementAsync(UpdateElementDTO dto)
    {
        var e = await _context.ItemElementSettings.FirstAsync(x => x.Id == dto.Id);
        e.ElementType = dto.ElementType;

        await _context.SaveChangesAsync();
        await LogAsync("UPDATE_ELEMENT", e.Id);
    }

    public async Task UpdateWeightAsync(UpdateTypeWeightDTO dto)
    {
        Validate(dto.WeaponType, dto.ArmorType);

        var w = await _context.ItemTypeWeights.FirstAsync(x => x.Id == dto.Id);

        w.Category = dto.Category;
        w.WeaponType = dto.WeaponType;
        w.ArmorType = dto.ArmorType;
        w.Weight = dto.Weight;

        await _context.SaveChangesAsync();
        await LogAsync("UPDATE_WEIGHT", w.Id);
    }

    #endregion

    #region DELETE

    public async Task DeleteProfileAsync(Guid id)
    {
        _context.GenerationProfiles.Remove(
            await _context.GenerationProfiles.FirstAsync(x => x.Id == id));

        await _context.SaveChangesAsync();
        await LogAsync("DELETE_PROFILE", id);
    }

    public async Task DeleteRuleAsync(Guid id)
    {
        _context.ItemGenerationRules.Remove(
            await _context.ItemGenerationRules.FirstAsync(x => x.Id == id));

        await _context.SaveChangesAsync();
        await LogAsync("DELETE_RULE", id);
    }

    public async Task DeleteParameterAsync(Guid id)
    {
        _context.ItemParameterSettings.Remove(
            await _context.ItemParameterSettings.FirstAsync(x => x.Id == id));

        await _context.SaveChangesAsync();
        await LogAsync("DELETE_PARAMETER", id);
    }

    public async Task DeleteElementAsync(Guid id)
    {
        _context.ItemElementSettings.Remove(
            await _context.ItemElementSettings.FirstAsync(x => x.Id == id));

        await _context.SaveChangesAsync();
        await LogAsync("DELETE_ELEMENT", id);
    }

    public async Task DeleteWeightAsync(Guid id)
    {
        _context.ItemTypeWeights.Remove(
            await _context.ItemTypeWeights.FirstAsync(x => x.Id == id));

        await _context.SaveChangesAsync();
        await LogAsync("DELETE_WEIGHT", id);
    }

    #endregion

    private void Validate(WeaponType? w, ArmorType? a)
    {
        if (w.HasValue && a.HasValue)
            throw new InvalidOperationException("Cannot be both weapon and armor");

        if (!w.HasValue && !a.HasValue)
            throw new InvalidOperationException("Must select weapon or armor");
    }

    private void ValidateProfile(CreateGenerationProfileFullDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Name required");

        if (dto.Rules.Any(r => r.WeaponType.HasValue && r.ArmorType.HasValue))
            throw new InvalidOperationException("Rule cannot contain both weapon and armor");

        if (dto.TypeWeights.Any(w => w.WeaponType.HasValue && w.ArmorType.HasValue))
            throw new InvalidOperationException("Weight cannot contain both weapon and armor");
    }

    private async Task LogAsync(string action, Guid id)
    {
        _context.AdminLogs.Add(new AdminLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            TargetUserId = id.ToString(),
            AdminId = Guid.Empty
        });

        await _context.SaveChangesAsync();
    }
}