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

        var itemCategory = SampleCategory(typeWeights);

        if (itemCategory == ItemCategory.Weapon)
        {
            var rule = profile.Rules.FirstOrDefault(r => r.Category == ItemCategory.Weapon)
                       ?? await GetFallbackRuleAsync(ItemCategory.Weapon);

            return GenerateWeapon(rule);
        }
        else
        {
            var rule = profile.Rules.FirstOrDefault(r => r.Category == ItemCategory.Armor)
                       ?? await GetFallbackRuleAsync(ItemCategory.Armor);

            return GenerateArmor(rule);
        }
    }

    public async Task<List<Item>> GenerateForEnemyAsync(Guid generationProfileId)
    {
        var profile = await _context.GenerationProfiles
            .Include(p => p.TypeWeights)
            .FirstOrDefaultAsync(p => p.Id == generationProfileId);

        if (profile == null)
            throw new InvalidOperationException("Generation profile not found");

        if (!profile.TypeWeights.Any())
            throw new InvalidOperationException("Profile has no TypeWeights");

        var result = new List<Item>();
        int remainingSlots = 4;

        var orderedTypes = profile.TypeWeights
            .OrderByDescending(x => x.Weight)
            .ToList();

        foreach (var typeWeight in orderedTypes)
        {
            if (remainingSlots <= 0)
                break;

            var category = SampleCategory(new List<ItemTypeWeight> { typeWeight });

            if (category == ItemCategory.Weapon)
            {
                var rule = await GetEnemyRuleAsync(generationProfileId, ItemCategory.Weapon);

                var weapon = GenerateWeapon(rule);

                var isTwoHanded = weapon.WeaponType.IsTwoHanded();

                if (isTwoHanded)
                {
                    if (remainingSlots < 2)
                        continue;

                    result.Add(weapon);
                    remainingSlots -= 2;
                }
                else
                {
                    result.Add(weapon);
                    remainingSlots -= 1;
                }
            }
            else
            {
                var rule = await GetEnemyRuleAsync(generationProfileId, ItemCategory.Armor);

                var armor = GenerateArmor(rule);

                result.Add(armor);
            }
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
    private async Task<ItemGenerationRule> GetEnemyRuleAsync(Guid generationProfileId, ItemCategory category)
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

        var rule = profile.Rules
            .FirstOrDefault(r => r.Category == category);

        if (rule == null)
            throw new InvalidOperationException("No rule for category in generation profile");

        return rule;
    }
}