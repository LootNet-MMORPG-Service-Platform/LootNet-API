using LootNet_API.Data;
using LootNet_API.DTO.Items;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ItemCollectionDTO> GetItemsAsync(Guid userId)
    {
        var weapons = await _context.Weapons
            .Where(x => x.OwnerId == userId)
            .Include(x => x.Elements)
            .ToListAsync();

        var armors = await _context.Armors
            .Where(x => x.OwnerId == userId)
            .Include(x => x.Elements)
            .ToListAsync();

        return new ItemCollectionDTO
        {
            Weapons = weapons.Select(MapWeapon).ToList(),
            Armors = armors.Select(MapArmor).ToList()
        };
    }

    public async Task<ItemCollectionDTO> GetInventoryAsync(Guid userId)
    {
        var eq = await _context.Equipments.FirstOrDefaultAsync(x => x.UserId == userId);

        var equipped = new HashSet<Guid>();

        if (eq != null)
        {
            var ids = new Guid?[]
            {
                eq.HeadId, eq.BodyId, eq.GlovesId, eq.LegsId, eq.BootsId,
                eq.WeaponSlot1Id, eq.WeaponSlot2Id, eq.WeaponSlot3Id, eq.WeaponSlot4Id
            };

            equipped = ids.Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToHashSet();
        }

        var weapons = await _context.Weapons
            .Where(x => x.OwnerId == userId && !equipped.Contains(x.Id))
            .Include(x => x.Elements)
            .ToListAsync();

        var armors = await _context.Armors
            .Where(x => x.OwnerId == userId && !equipped.Contains(x.Id))
            .Include(x => x.Elements)
            .ToListAsync();

        return new ItemCollectionDTO
        {
            Weapons = weapons.Select(MapWeapon).ToList(),
            Armors = armors.Select(MapArmor).ToList()
        };
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

    private async Task<WeaponDTO?> GetWeapon(Guid? id)
    {
        if (!id.HasValue) return null;

        var item = await _context.Weapons
            .Include(x => x.Elements)
            .FirstOrDefaultAsync(x => x.Id == id.Value);

        return item == null ? null : MapWeapon(item);
    }

    private async Task<ArmorDTO?> GetArmor(Guid? id)
    {
        if (!id.HasValue) return null;

        var item = await _context.Armors
            .Include(x => x.Elements)
            .FirstOrDefaultAsync(x => x.Id == id.Value);

        return item == null ? null : MapArmor(item);
    }

    private WeaponDTO MapWeapon(Weapon x)
    {
        return new WeaponDTO
        {
            Id = x.Id,
            Name = x.Name,
            Category = x.Category,
            WeaponType = x.WeaponType,
            Cut = x.Cut,
            Blunt = x.Blunt,
            Elements = x.Elements.Select(e => new ItemElementDTO
            {
                Type = e.ItemElementType,
                Value = e.Value
            }).ToList()
        };
    }

    private ArmorDTO MapArmor(Armor x)
    {
        return new ArmorDTO
        {
            Id = x.Id,
            Name = x.Name,
            Category = x.Category,
            ArmorType = x.ArmorType,
            CutResistance = x.CutResistance,
            BluntResistance = x.BluntResistance,
            Elements = x.Elements.Select(e => new ItemElementDTO
            {
                Type = e.ItemElementType,
                Value = e.Value
            }).ToList()
        };
    }


    public async Task EquipWeaponAsync(Guid userId, Guid itemId, int slot)
    {
        var weapon = await _context.Weapons
            .FirstOrDefaultAsync(x => x.Id == itemId && x.OwnerId == userId);

        if (weapon == null)
            throw new InvalidOperationException("Weapon not found");

        var eq = await _context.Equipments
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (eq == null)
            throw new InvalidOperationException("Equipment not found");

        switch (slot)
        {
            case 1: eq.WeaponSlot1Id = itemId; break;
            case 2: eq.WeaponSlot2Id = itemId; break;
            case 3: eq.WeaponSlot3Id = itemId; break;
            case 4: eq.WeaponSlot4Id = itemId; break;
            default: throw new InvalidOperationException("Invalid weapon slot");
        }

        await _context.SaveChangesAsync();
    }

    public async Task EquipArmorAsync(Guid userId, Guid itemId)
    {
        var armor = await _context.Armors
            .FirstOrDefaultAsync(x => x.Id == itemId && x.OwnerId == userId);

        if (armor == null)
            throw new InvalidOperationException("Armor not found");

        var eq = await _context.Equipments
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (eq == null)
            throw new InvalidOperationException("Equipment not found");

        switch (armor.ArmorType)
        {
            case ArmorType.Head:
                eq.HeadId = itemId;
                break;

            case ArmorType.Body:
                eq.BodyId = itemId;
                break;

            case ArmorType.Gloves:
                eq.GlovesId = itemId;
                break;

            case ArmorType.Legs:
                eq.LegsId = itemId;
                break;

            case ArmorType.Boots:
                eq.BootsId = itemId;
                break;

            default:
                throw new InvalidOperationException("Invalid armor category");
        }

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
}