using LootNet_API.Data;
using LootNet_API.Enums;
using LootNet_API.Models.Items;
using LootNet_API.Models.Items.Generation;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class ItemGenerationService : IItemGenerationService
{
    private readonly AppDbContext _context;
    private readonly Random _rand = new();
    private readonly IItemNameGenerator _nameGenerator;

    public ItemGenerationService(AppDbContext context, IItemNameGenerator nameGenerator)
    {
        _context = context;
        _nameGenerator = nameGenerator;
    }

    public async Task<Item> GenerateItemAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Profile)
                .ThenInclude(p => p.Rules)
                    .ThenInclude(r => r.Parameters)
                        .ThenInclude(p => p.Segments)
            .Include(u => u.Profile)
                .ThenInclude(p => p.Rules)
                    .ThenInclude(r => r.Elements)
                        .ThenInclude(e => e.Segments)
            .Include(u => u.Profile)
                .ThenInclude(p => p.TypeWeights)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) throw new InvalidOperationException("User not found");
        var profile = user.Profile ?? throw new InvalidOperationException("User has no profile");
        var typeWeights = profile.TypeWeights;
        if (!typeWeights.Any()) throw new InvalidOperationException("Profile has no TypeWeights");

        var selectedType = SampleTypeWeight(typeWeights);
        var rule = ResolveRuleForType(profile.Rules, selectedType)
                   ?? await GetFallbackRuleAsync(selectedType.Category);

        return selectedType.Category == ItemCategory.Weapon
            ? GenerateWeapon(rule)
            : GenerateArmor(rule);
    }

    public async Task<List<Item>> GenerateForEnemyAsync(Guid generationProfileId)
    {
        var profile = await _context.GenerationProfiles
            .Include(p => p.Rules)
                .ThenInclude(r => r.Parameters)
                    .ThenInclude(p => p.Segments)
            .Include(p => p.Rules)
                .ThenInclude(r => r.Elements)
                    .ThenInclude(e => e.Segments)
            .Include(p => p.TypeWeights)
            .FirstOrDefaultAsync(p => p.Id == generationProfileId);

        if (profile == null)
            throw new InvalidOperationException("Generation profile not found");

        if (!profile.TypeWeights.Any())
            throw new InvalidOperationException("Profile has no TypeWeights");

        var result = new List<Item>();

        var weaponWeights = profile.TypeWeights
            .Where(x => x.Category == ItemCategory.Weapon)
            .ToList();
        if (weaponWeights.Count > 0)
        {
            var selectedWeaponWeight = SampleTypeWeight(weaponWeights);
            var weaponRule = ResolveRuleForType(profile.Rules, selectedWeaponWeight)
                             ?? await GetFallbackRuleAsync(ItemCategory.Weapon);
            result.Add(GenerateWeapon(weaponRule));
        }

        var armorWeights = profile.TypeWeights
            .Where(x => x.Category == ItemCategory.Armor && x.ArmorType.HasValue)
            .OrderByDescending(x => x.Weight)
            .GroupBy(x => x.ArmorType!.Value)
            .Select(x => x.First())
            .ToList();

        foreach (var armorWeight in armorWeights)
        {
            var armorRule = ResolveRuleForType(profile.Rules, armorWeight)
                            ?? await GetFallbackRuleAsync(ItemCategory.Armor);
            result.Add(GenerateArmor(armorRule));
        }

        return result;
    }

    private ItemCategory SampleCategory(ICollection<ItemTypeWeight> typeWeights)
    {
        double total = typeWeights.Sum(t => t.Weight);
        double roll = _rand.NextDouble() * total;
        double cumulative = 0;

        foreach (var t in typeWeights)
        {
            cumulative += t.Weight;
            if (roll <= cumulative) return t.Category;
        }

        return typeWeights.Last().Category;
    }

    private ItemTypeWeight SampleTypeWeight(ICollection<ItemTypeWeight> typeWeights)
    {
        double total = typeWeights.Sum(t => t.Weight);
        double roll = _rand.NextDouble() * total;
        double cumulative = 0;

        foreach (var t in typeWeights)
        {
            cumulative += t.Weight;
            if (roll <= cumulative) return t;
        }

        return typeWeights.Last();
    }

    private async Task<ItemGenerationRule> GetFallbackRuleAsync(ItemCategory category)
    {
        return await _context.ItemGenerationRules
            .Include(r => r.Parameters)
                .ThenInclude(p => p.Segments)
            .Include(r => r.Elements)
                .ThenInclude(e => e.Segments)
            .FirstOrDefaultAsync(r => r.IsFallback && r.Category == category)
            ?? throw new InvalidOperationException("No fallback for category");
    }

    private Weapon GenerateWeapon(ItemGenerationRule rule)
    {
        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            Category = ItemCategory.Weapon,
            WeaponType = rule.WeaponType!.Value,
            Name = _nameGenerator.GenerateWeaponName(rule.WeaponType.Value)
        };

        foreach (var param in rule.Parameters)
        {
            switch (param.Parameter)
            {
                case ItemParameter.CutDamage:
                    weapon.Cut = SampleParam(param);
                    break;

                case ItemParameter.BluntDamage:
                    weapon.Blunt = SampleParam(param);
                    break;
            }
        }

        foreach (var elem in rule.Elements)
        {
            weapon.Elements.Add(new ItemElement
            {
                ItemElementType = elem.ElementType,
                Value = SampleParam(elem)
            });
        }

        return weapon;
    }

    private Armor GenerateArmor(ItemGenerationRule rule)
    {
        var armor = new Armor
        {
            Id = Guid.NewGuid(),
            Category = ItemCategory.Armor,
            ArmorType = rule.ArmorType!.Value,
            Name = _nameGenerator.GenerateArmorName(rule.ArmorType.Value)
        };

        foreach (var param in rule.Parameters)
        {
            switch (param.Parameter)
            {
                case ItemParameter.CutResistance:
                    armor.CutResistance = SampleParam(param);
                    break;

                case ItemParameter.BluntResistance:
                    armor.BluntResistance = SampleParam(param);
                    break;
            }
        }

        foreach (var elem in rule.Elements)
        {
            armor.Elements.Add(new ItemElement
            {
                ItemElementType = elem.ElementType,
                Value = SampleParam(elem)
            });
        }

        return armor;
    }

    private double SampleParam(ItemParameterSetting param)
    {
        double totalWeight = param.Segments.Sum(s => s.Weight);
        double roll = _rand.NextDouble() * totalWeight;
        double cumulative = 0;

        foreach (var seg in param.Segments)
        {
            cumulative += seg.Weight;
            if (roll <= cumulative)
                return _rand.NextDouble() * (seg.Max - seg.Min) + seg.Min;
        }

        var last = param.Segments.Last();
        return _rand.NextDouble() * (last.Max - last.Min) + last.Min;
    }

    private double SampleParam(ItemElementSetting elem)
    {
        double totalWeight = elem.Segments.Sum(s => s.Weight);
        double roll = _rand.NextDouble() * totalWeight;
        double cumulative = 0;

        foreach (var seg in elem.Segments)
        {
            cumulative += seg.Weight;
            if (roll <= cumulative)
                return _rand.NextDouble() * (seg.Max - seg.Min) + seg.Min;
        }

        var last = elem.Segments.Last();
        return _rand.NextDouble() * (last.Max - last.Min) + last.Min;
    }
    private ItemGenerationRule? ResolveRuleForType(IEnumerable<ItemGenerationRule> rules, ItemTypeWeight typeWeight)
    {
        var filtered = rules.Where(r => r.Category == typeWeight.Category);

        if (typeWeight.Category == ItemCategory.Weapon && typeWeight.WeaponType.HasValue)
        {
            var exactWeapon = filtered.FirstOrDefault(r => !r.IsFallback && r.WeaponType == typeWeight.WeaponType);
            if (exactWeapon != null) return exactWeapon;
        }

        if (typeWeight.Category == ItemCategory.Armor && typeWeight.ArmorType.HasValue)
        {
            var exactArmor = filtered.FirstOrDefault(r => !r.IsFallback && r.ArmorType == typeWeight.ArmorType);
            if (exactArmor != null) return exactArmor;
        }

        var generic = filtered.FirstOrDefault(r => !r.IsFallback);
        if (generic != null) return generic;

        return filtered.FirstOrDefault(r => r.IsFallback);
    }

    private async Task<ItemGenerationRule> GetEnemyRuleAsync(Guid generationProfileId, ItemTypeWeight typeWeight)
    {
        var profile = await _context.GenerationProfiles
            .Include(p => p.Rules)
                .ThenInclude(r => r.Parameters)
                    .ThenInclude(p => p.Segments)
            .Include(p => p.Rules)
                .ThenInclude(r => r.Elements)
                    .ThenInclude(e => e.Segments)
            .FirstOrDefaultAsync(p => p.Id == generationProfileId);

        if (profile == null)
            throw new InvalidOperationException("Generation profile not found");

        var rule = ResolveRuleForType(profile.Rules, typeWeight);

        if (rule == null)
            throw new InvalidOperationException("No matching generation rule in profile");

        return rule;
    }
}
