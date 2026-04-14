using LootNet_API.Enums;
using LootNet_API.Services.Interfaces;

namespace LootNet_API.Services;

public class ItemNameGenerator : IItemNameGenerator
{
    private readonly Random _rand = new();

    private readonly List<string> prefixes = new()
    {
        "Ancient", "Brutal", "Cursed", "Forgotten", "Savage"
    };

    private readonly List<string> suffixes = new()
    {
        "of Doom", "of Fury", "of the Fox", "of Storms", "of Blood"
    };

    public string GenerateWeaponName(WeaponType type)
    {
        var prefix = prefixes[_rand.Next(prefixes.Count)];
        var suffix = suffixes[_rand.Next(suffixes.Count)];

        return $"{prefix} {type} {suffix}";
    }

    public string GenerateArmorName(ArmorType type)
    {
        var prefix = prefixes[_rand.Next(prefixes.Count)];
        var suffix = suffixes[_rand.Next(suffixes.Count)];

        return $"{prefix} {type} {suffix}";
    }
}
