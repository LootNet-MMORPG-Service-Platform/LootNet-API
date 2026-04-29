using LootNet_API.Enums;
using LootNet_API.Models.GameRun;
using LootNet_API.Models.Items;

namespace LootNet_API.Services;

public class CombatHandsService
{
    public void InitializePlayerHands(Run run, List<Weapon> weapons)
    {
        run.LeftHandItemId = null;
        run.RightHandItemId = null;

        if (weapons == null || weapons.Count == 0)
            return;

        var first = weapons[0];

        if (first.WeaponType.IsTwoHanded())
        {
            EquipTwoHand(run, first.Id);
            return;
        }

        run.LeftHandItemId = first.Id;

        for (int i = 1; i < weapons.Count; i++)
        {
            var weapon = weapons[i];

            if (weapon.WeaponType.IsTwoHanded())
                continue;

            run.RightHandItemId = weapon.Id;
            break;
        }
    }

    public void InitializeEnemyHands(BattleEnemy enemy, List<Weapon> weapons)
    {
        enemy.LeftHandItemId = null;
        enemy.RightHandItemId = null;

        if (weapons == null || weapons.Count == 0)
            return;

        //var ordered = ApplyWeaponPriority(weapons);

        var first = weapons[0];

        if (first.WeaponType.IsTwoHanded())
        {
            EquipTwoHand(enemy, first.Id);
            return;
        }

        enemy.LeftHandItemId = first.Id;

        for (int i = 1; i < weapons.Count; i++)
        {
            var weapon = weapons[i];

            if (weapon.WeaponType.IsTwoHanded())
                continue;

            enemy.RightHandItemId = weapon.Id;
            break;
        }
    }

    public void ChangePlayerHands(Run run, Weapon? left, Weapon? right)
    {
        ApplyChange(run, left, right);
    }

    public void ChangeEnemyHands(BattleEnemy enemy, Weapon? left, Weapon? right)
    {
        ApplyChange(enemy, left, right);
    }

    private void ApplyChange(dynamic target, Weapon? left, Weapon? right)
    {
        if (left != null && left.WeaponType.IsTwoHanded())
        {
            EquipTwoHand(target, left.Id);
            return;
        }

        if (right != null && right.WeaponType.IsTwoHanded())
        {
            EquipTwoHand(target, right.Id);
            return;
        }

        if (IsTwoHandEquipped(target))
        {
            ClearHands(target);
        }

        if (left != null)
            target.LeftHandItemId = left.Id;

        if (right != null)
            target.RightHandItemId = right.Id;
    }

    private bool IsTwoHandEquipped(dynamic target)
    {
        return target.LeftHandItemId != null &&
               target.LeftHandItemId == target.RightHandItemId;
    }

    private void ClearHands(dynamic target)
    {
        target.LeftHandItemId = null;
        target.RightHandItemId = null;
    }

    private void EquipTwoHand(dynamic target, Guid weaponId)
    {
        target.LeftHandItemId = weaponId;
        target.RightHandItemId = weaponId;
    }
}