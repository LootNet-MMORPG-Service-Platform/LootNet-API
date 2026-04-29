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
}