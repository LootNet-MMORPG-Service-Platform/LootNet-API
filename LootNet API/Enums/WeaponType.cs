namespace LootNet_API.Enums;

public enum WeaponType
{
    TwoHandSword,
    Sword,
    Shortsword,
    Polearm,
    Crossbow,
    Bow
}

public static class WeaponTypeExtensions
{
    public static bool IsTwoHanded(this WeaponType type)
    {
        return type switch
        {
            WeaponType.TwoHandSword => true,
            WeaponType.Bow => true,
            WeaponType.Crossbow => true,
            _ => false
        };
    }
    public static bool IsRanged(this WeaponType type)
    => type is WeaponType.Bow or WeaponType.Crossbow;

    public static bool IsMelee(this WeaponType type)
        => !IsRanged(type);
}
