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

    public async Task MoveToRunAsync(Guid userId, List<Guid> itemIds)
    {
        var items = await _context.InventoryItems
            .Where(x => x.UserId == userId && itemIds.Contains(x.ItemId))
            .ToListAsync();

        foreach (var i in items)
        {
            _context.RunInventoryItems.Add(new RunInventoryItem
            {
                Id = Guid.NewGuid(),
                UserId = i.UserId,
                ItemId = i.ItemId
            });
        }

        _context.InventoryItems.RemoveRange(items);

        await _context.SaveChangesAsync();
    }

    public async Task ReturnFromRunAsync(Guid userId)
    {
        var items = await _context.RunInventoryItems
            .Where(x => x.UserId == userId)
            .ToListAsync();

        foreach (var i in items)
        {
            _context.InventoryItems.Add(new InventoryItem
            {
                Id = Guid.NewGuid(),
                UserId = i.UserId,
                ItemId = i.ItemId
            });
        }

        _context.RunInventoryItems.RemoveRange(items);

        await _context.SaveChangesAsync();
    }

    public async Task MoveToMarketAsync(Guid userId, Guid itemId)
    {
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId);

        if (item == null)
            throw new InvalidOperationException("Item not in inventory");

        _context.MarketInventoryItems.Add(new MarketInventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemId = itemId
        });

        _context.InventoryItems.Remove(item);

        await _context.SaveChangesAsync();
    }

    public async Task ReturnFromMarketAsync(Guid userId, Guid itemId)
    {
        var item = await _context.MarketInventoryItems
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId);

        if (item == null)
            throw new InvalidOperationException();

        _context.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemId = itemId
        });

        _context.MarketInventoryItems.Remove(item);

        await _context.SaveChangesAsync();
    }

    public async Task TransferFromSellerToBuyerAsync(Guid sellerId, Guid buyerId, Guid itemId)
    {
        var listing = await _context.MarketInventoryItems
            .FirstOrDefaultAsync(x => x.UserId == sellerId && x.ItemId == itemId);

        if (listing == null)
            throw new InvalidOperationException();

        _context.MarketInventoryItems.Remove(listing);

        _context.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = buyerId,
            ItemId = itemId
        });

        await _context.SaveChangesAsync();
    }

    public async Task<ItemCollectionDTO> GetItemsAsync(Guid userId)
    {
        var itemIds = await _context.InventoryItems
            .Where(x => x.UserId == userId)
            .Select(x => x.ItemId)
            .ToListAsync();

        var weapons = await _context.Weapons
            .Where(x => itemIds.Contains(x.Id))
            .Include(x => x.Elements)
            .ToListAsync();

        var armors = await _context.Armors
            .Where(x => itemIds.Contains(x.Id))
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
        var ids = await _context.InventoryItems
            .Where(x => x.UserId == userId)
            .Select(x => x.ItemId)
            .ToListAsync();

        return await LoadItems(ids);
    }

    public async Task<ItemCollectionDTO> GetRunInventoryAsync(Guid userId)
    {
        var ids = await _context.RunInventoryItems
            .Where(x => x.UserId == userId)
            .Select(x => x.ItemId)
            .ToListAsync();

        return await LoadItems(ids);
    }

    public async Task<ItemCollectionDTO> GetMarketInventoryAsync(Guid userId)
    {
        var ids = await _context.MarketInventoryItems
            .Where(x => x.UserId == userId)
            .Select(x => x.ItemId)
            .ToListAsync();

        return await LoadItems(ids);
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


    public async Task EquipWeaponAsync(Guid userId, Guid itemId, int slot)
    {
        var exists = await _context.InventoryItems
            .AnyAsync(x => x.UserId == userId && x.ItemId == itemId);

        if (!exists)
            throw new InvalidOperationException("Item not in inventory");

        var eq = await _context.Equipments
            .FirstAsync(x => x.UserId == userId);

        SetWeaponSlot(eq, itemId, slot);

        await _context.SaveChangesAsync();
    }

    public async Task EquipArmorAsync(Guid userId, Guid itemId)
    {
        var exists = await _context.InventoryItems
            .AnyAsync(x => x.UserId == userId && x.ItemId == itemId);

        if (!exists)
            throw new InvalidOperationException("Item not in inventory");

        var armor = await _context.Armors
            .FirstOrDefaultAsync(x => x.Id == itemId);

        if (armor == null)
            throw new InvalidOperationException("Armor not found");

        var eq = await _context.Equipments
            .FirstAsync(x => x.UserId == userId);

        SetArmorSlot(eq, armor.ArmorType, itemId);

        await _context.SaveChangesAsync();
    }

    public async Task EquipWeaponFromRunAsync(Guid userId, Guid itemId, int slot)
    {
        var exists = await _context.RunInventoryItems
            .AnyAsync(x => x.UserId == userId && x.ItemId == itemId);

        if (!exists)
            throw new InvalidOperationException("Item not in run inventory");

        var eq = await _context.Equipments.FirstAsync(x => x.UserId == userId);

        SetWeaponSlot(eq, itemId, slot);

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

    public async Task AddToInventoryAsync(Guid userId, Guid itemId)
    {
        _context.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemId = itemId
        });

        await _context.SaveChangesAsync();
    }

    public async Task AddToRunInventoryAsync(Guid userId, Guid itemId)
    {
        _context.RunInventoryItems.Add(new RunInventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemId = itemId
        });

        await _context.SaveChangesAsync();
    }

    private async Task<ItemCollectionDTO> LoadItems(List<Guid> ids)
    {
        var weapons = await _context.Weapons
            .Where(x => ids.Contains(x.Id))
            .Include(x => x.Elements)
            .ToListAsync();

        var armors = await _context.Armors
            .Where(x => ids.Contains(x.Id))
            .Include(x => x.Elements)
            .ToListAsync();

        return new ItemCollectionDTO
        {
            Weapons = weapons.Select(MapWeapon).ToList(),
            Armors = armors.Select(MapArmor).ToList()
        };
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