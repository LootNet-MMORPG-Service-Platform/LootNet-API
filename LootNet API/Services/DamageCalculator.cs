using LootNet_API.DTO.Items;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.GameRun;
using LootNet_API.Services.Interfaces;

namespace LootNet_API.Services;

public class DamageCalculator
{
    private readonly IEquipmentService _equipment;

    private const double DisorganizedMultiplier = 1.35;
    private const double MeleeVsRangedMultiplier = 1.50;
    private const double RangedVsMeleePenalty = 0.65;

    public DamageCalculator(IEquipmentService equipment)
    {
        _equipment = equipment;
    }

    public async Task<int> CalculatePlayerToEnemyAsync(Run run, Equipment playerEquipment, BattleEnemy enemy, bool isRangedAttack)
    {
        var leftWeapon = await _equipment.GetWeapon(run.LeftHandItemId);
        var rightWeapon = await _equipment.GetWeapon(run.RightHandItemId);

        if (leftWeapon == null && rightWeapon == null)
            return 1;

        var armor =
            await _equipment.GetArmor(enemy.Equipment.HeadId) ??
            await _equipment.GetArmor(enemy.Equipment.BodyId) ??
            await _equipment.GetArmor(enemy.Equipment.GlovesId) ??
            await _equipment.GetArmor(enemy.Equipment.LegsId) ??
            await _equipment.GetArmor(enemy.Equipment.BootsId) ??
            new ArmorDTO { Name = "", CutResistance = 0, BluntResistance = 0, Elements = new List<ItemElementDTO>() };

        var enemyLeftWeapon = await _equipment.GetWeapon(enemy.LeftHandItemId);
        var enemyRightWeapon = await _equipment.GetWeapon(enemy.RightHandItemId);
        var defenderHasMelee =
            (enemyLeftWeapon != null && enemyLeftWeapon.WeaponType.IsMelee()) ||
            (enemyRightWeapon != null && enemyRightWeapon.WeaponType.IsMelee());

        var weapons = new[] { leftWeapon, rightWeapon }
            .Where(w => w != null && (isRangedAttack ? w.WeaponType.IsRanged() : w.WeaponType.IsMelee()))
            .Cast<WeaponDTO>();

        var damage = weapons.Sum(w => Calculate(w, armor, run.IsPlayerDisorganized, defenderHasMelee, isRangedAttack));

        return Math.Max(1, (int)Math.Round(damage));
    }

    public async Task<int> CalculateEnemyToPlayerAsync(BattleEnemy enemy, Run run, Equipment playerEquipment, bool isRangedAttack)
    {
        var leftWeapon = await _equipment.GetWeapon(enemy.LeftHandItemId);
        var rightWeapon = await _equipment.GetWeapon(enemy.RightHandItemId);

        if (leftWeapon == null && rightWeapon == null)
            return 1;

        var armor =
            await _equipment.GetArmor(playerEquipment.HeadId) ??
            await _equipment.GetArmor(playerEquipment.BodyId) ??
            await _equipment.GetArmor(playerEquipment.GlovesId) ??
            await _equipment.GetArmor(playerEquipment.LegsId) ??
            await _equipment.GetArmor(playerEquipment.BootsId) ??
            new ArmorDTO { Name = "", CutResistance = 0, BluntResistance = 0, Elements = new List<ItemElementDTO>() };

        var playerLeftWeapon = await _equipment.GetWeapon(run.LeftHandItemId);
        var playerRightWeapon = await _equipment.GetWeapon(run.RightHandItemId);
        var defenderHasMelee =
            (playerLeftWeapon != null && playerLeftWeapon.WeaponType.IsMelee()) ||
            (playerRightWeapon != null && playerRightWeapon.WeaponType.IsMelee());

        var weapons = new[] { leftWeapon, rightWeapon }
            .Where(w => w != null && (isRangedAttack ? w.WeaponType.IsRanged() : w.WeaponType.IsMelee()))
            .Cast<WeaponDTO>();

        var damage = weapons.Sum(w => Calculate(w, armor, enemy.IsDisorganized, defenderHasMelee, isRangedAttack));

        return Math.Max(1, (int)Math.Round(damage));
    }

    private double Calculate(WeaponDTO weapon, ArmorDTO armor, bool isDisorganized, bool defenderHasMelee, bool isRangedAttack)
    {
        var cut = weapon.Cut * 100d / (100d + armor.CutResistance);
        var blunt = weapon.Blunt * 100d / (100d + armor.BluntResistance);
        var damage = cut + blunt;

        var atk = new double[4];
        var def = new double[4];

        foreach (var e in weapon.Elements)
            atk[(int)e.Type] += e.Value;

        foreach (var e in armor.Elements)
            def[(int)e.Type] += e.Value;

        for (int i = 0; i < 4; i++)
        {
            damage -= Math.Min(atk[i], def[i]) * 0.35;

            var opposite = (i + 2) % 4;
            var net = atk[i] - def[opposite];
            damage += net > 0 ? net * 0.60 : net * 0.45;

            var wins = (i + 1) % 4;
            var netW = atk[i] - def[wins];
            damage += netW > 0 ? netW * 0.40 : netW * 0.30;
        }

        if (isDisorganized)
            damage *= DisorganizedMultiplier;

        if (isRangedAttack && defenderHasMelee)
            damage *= RangedVsMeleePenalty;

        if (!isRangedAttack && !defenderHasMelee)
            damage *= MeleeVsRangedMultiplier;

        return damage;
    }
}