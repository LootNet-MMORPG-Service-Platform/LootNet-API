using LootNet_API.Data;
using LootNet_API.DTO.Items;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class EquipmentService : IEquipmentService
{
    private readonly AppDbContext _context;

    public EquipmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EquipmentResponseDTO> GetEquipmentAsync(Guid userId)
    {
        var eq = await _context.Equipments.FirstOrDefaultAsync(x => x.UserId == userId);

        if (eq == null)
            return new EquipmentResponseDTO();

        return new EquipmentResponseDTO
        {
            Head = await GetArmor(eq.HeadId),
            Body = await GetArmor(eq.BodyId),
            Gloves = await GetArmor(eq.GlovesId),
            Legs = await GetArmor(eq.LegsId),
            Boots = await GetArmor(eq.BootsId),

            Weapon1 = await GetWeapon(eq.WeaponSlot1Id),
            Weapon2 = await GetWeapon(eq.WeaponSlot2Id),
            Weapon3 = await GetWeapon(eq.WeaponSlot3Id),
            Weapon4 = await GetWeapon(eq.WeaponSlot4Id)
        };
    }

    public async Task EquipWeaponAsync(Guid userId, Guid itemId, int slot)
    {
        var exists = await _context.InventoryItems
            .AnyAsync(x => x.UserId == userId && x.ItemId == itemId);

        if (!exists)
            throw new InvalidOperationException("Item not in inventory");

        var weapon = await _context.Weapons.FirstOrDefaultAsync(x => x.Id == itemId);
        if (weapon == null)
            throw new InvalidOperationException("Weapon not found");

        var eq = await _context.Equipments.FirstAsync(x => x.UserId == userId);

        ApplyWeapon(eq, weapon, slot);

        await _context.SaveChangesAsync();
    }

    public async Task EquipWeaponFromRunAsync(Guid userId, Guid itemId, int slot)
    {
        var exists = await _context.RunInventoryItems
            .AnyAsync(x => x.UserId == userId && x.ItemId == itemId);

        if (!exists)
            throw new InvalidOperationException("Item not in run inventory");

        var weapon = await _context.Weapons.FirstAsync(x => x.Id == itemId);
        var eq = await _context.Equipments.FirstAsync(x => x.UserId == userId);

        ApplyWeapon(eq, weapon, slot);

        await _context.SaveChangesAsync();
    }

    public async Task EquipArmorAsync(Guid userId, Guid itemId)
    {
        var exists = await _context.InventoryItems
            .AnyAsync(x => x.UserId == userId && x.ItemId == itemId);

        if (!exists)
            throw new InvalidOperationException("Item not in inventory");

        var armor = await _context.Armors.FirstOrDefaultAsync(x => x.Id == itemId);
        if (armor == null)
            throw new InvalidOperationException("Armor not found");

        var eq = await _context.Equipments.FirstAsync(x => x.UserId == userId);

        SetArmorSlot(eq, armor.ArmorType, itemId);

        await _context.SaveChangesAsync();
    }

    public async Task EquipArmorFromRunAsync(Guid userId, Guid itemId)
    {
        var exists = await _context.RunInventoryItems
            .AnyAsync(x => x.UserId == userId && x.ItemId == itemId);

        if (!exists)
            throw new InvalidOperationException();

        var armor = await _context.Armors.FirstAsync(x => x.Id == itemId);
        var eq = await _context.Equipments.FirstAsync(x => x.UserId == userId);

        SetArmorSlot(eq, armor.ArmorType, itemId);

        await _context.SaveChangesAsync();
    }

    public async Task UnequipItemAsync(Guid userId, Guid itemId)
    {
        var eq = await _context.Equipments.FirstOrDefaultAsync(x => x.UserId == userId);
        if (eq == null) return;

        var props = typeof(Equipment).GetProperties();

        foreach (var p in props)
        {
            if (p.PropertyType == typeof(Guid?) &&
                (Guid?)p.GetValue(eq) == itemId)
            {
                p.SetValue(eq, null);
                break;
            }
        }

        await _context.SaveChangesAsync();
    }

    public void ApplyEnemyEquipment(Equipment equipment, List<Item> items)
    {
        foreach (var item in items)
        {
            if (item.Category == ItemCategory.Armor)
            {
                var armor = item as Armor;
                if (armor == null) continue;

                SetArmorSlot(equipment, armor.ArmorType, armor.Id);
                continue;
            }

            if (item.Category == ItemCategory.Weapon)
            {
                var weapon = item as Weapon;
                if (weapon == null) continue;

                ApplyWeapon(equipment, weapon, 1);
            }
        }
    }

    private void ApplyWeapon(Equipment eq, Weapon weapon, int slot)
    {
        if (weapon.WeaponType.IsTwoHanded())
        {
            EquipTwoHand(eq, weapon.Id, slot);
            return;
        }

        SetWeaponSlot(eq, weapon.Id, slot);
    }

    private void EquipTwoHand(Equipment eq, Guid itemId, int slot)
    {
        if (slot == 4)
        {
            ClearIfTwoHand(eq.WeaponSlot3Id, eq);
            SetWeaponSlot(eq, itemId, 3);
            SetWeaponSlot(eq, itemId, 4);
            return;
        }

        if (slot == 1)
        {
            ClearIfTwoHand(eq.WeaponSlot1Id, eq);
            SetWeaponSlot(eq, itemId, 1);
            SetWeaponSlot(eq, itemId, 2);
            return;
        }

        if (slot == 2)
        {
            ClearIfTwoHand(eq.WeaponSlot2Id, eq);
            SetWeaponSlot(eq, itemId, 2);
            SetWeaponSlot(eq, itemId, 3);
            return;
        }

        ClearIfTwoHand(eq.WeaponSlot3Id, eq);
        SetWeaponSlot(eq, itemId, 3);
        SetWeaponSlot(eq, itemId, 4);
    }

    private void ClearIfTwoHand(Guid? existingWeaponId, Equipment eq)
    {
        if (!existingWeaponId.HasValue)
            return;

        var weapon = _context.Weapons.FirstOrDefault(x => x.Id == existingWeaponId.Value);

        if (weapon == null || !weapon.WeaponType.IsTwoHanded())
            return;

        ClearWeapon(eq, existingWeaponId.Value);
    }

    private void ClearWeapon(Equipment eq, Guid itemId)
    {
        if (eq.WeaponSlot1Id == itemId) eq.WeaponSlot1Id = null;
        if (eq.WeaponSlot2Id == itemId) eq.WeaponSlot2Id = null;
        if (eq.WeaponSlot3Id == itemId) eq.WeaponSlot3Id = null;
        if (eq.WeaponSlot4Id == itemId) eq.WeaponSlot4Id = null;
    }

    public async Task<WeaponDTO?> GetWeapon(Guid? id)
    {
        if (!id.HasValue) return null;

        var item = await _context.Weapons
            .Include(x => x.Elements)
            .FirstOrDefaultAsync(x => x.Id == id.Value);

        return item == null ? null : new WeaponDTO
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            Cut = item.Cut,
            Blunt = item.Blunt,
            Elements = item.Elements.Select(e => new ItemElementDTO
            {
                Type = e.ItemElementType,
                Value = e.Value
            }).ToList()
        };
    }

    public async Task<ArmorDTO?> GetArmor(Guid? id)
    {
        if (!id.HasValue) return null;

        var item = await _context.Armors
            .Include(x => x.Elements)
            .FirstOrDefaultAsync(x => x.Id == id.Value);

        return item == null ? null : new ArmorDTO
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            CutResistance = item.CutResistance,
            BluntResistance = item.BluntResistance,
            Elements = item.Elements.Select(e => new ItemElementDTO
            {
                Type = e.ItemElementType,
                Value = e.Value
            }).ToList()
        };
    }

    private void SetWeaponSlot(Equipment eq, Guid itemId, int slot)
    {
        switch (slot)
        {
            case 1: eq.WeaponSlot1Id = itemId; break;
            case 2: eq.WeaponSlot2Id = itemId; break;
            case 3: eq.WeaponSlot3Id = itemId; break;
            case 4: eq.WeaponSlot4Id = itemId; break;
            default: throw new InvalidOperationException();
        }
    }

    private void SetArmorSlot(Equipment eq, ArmorType type, Guid itemId)
    {
        switch (type)
        {
            case ArmorType.Head: eq.HeadId = itemId; break;
            case ArmorType.Body: eq.BodyId = itemId; break;
            case ArmorType.Gloves: eq.GlovesId = itemId; break;
            case ArmorType.Legs: eq.LegsId = itemId; break;
            case ArmorType.Boots: eq.BootsId = itemId; break;
            default: throw new InvalidOperationException();
        }
    }
}