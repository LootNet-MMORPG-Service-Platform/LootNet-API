using LootNet_API.Data;
using LootNet_API.DTO.Items;
using LootNet_API.Models.Items;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class InventoryService : IInventoryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IRealtimeNotifier? _realtimeNotifier;

    public InventoryService(IDbContextFactory<AppDbContext> dbFactory, IRealtimeNotifier? realtimeNotifier = null)
    {
        _dbFactory = dbFactory;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task MoveToRunAsync(Guid userId, List<Guid> itemIds)
    {
        await using var db = _dbFactory.CreateDbContext();

        var items = await db.InventoryItems
            .Where(x => x.UserId == userId && itemIds.Contains(x.ItemId))
            .ToListAsync();

        foreach (var i in items)
            db.RunInventoryItems.Add(new RunInventoryItem { Id = Guid.NewGuid(), UserId = i.UserId, ItemId = i.ItemId });

        db.InventoryItems.RemoveRange(items);
        await db.SaveChangesAsync();
        await NotifyAsync("inventory", "move-to-run", userId);
    }

    public async Task ReturnFromRunAsync(Guid userId)
    {
        await using var db = _dbFactory.CreateDbContext();

        var items = await db.RunInventoryItems
            .Where(x => x.UserId == userId)
            .ToListAsync();

        foreach (var i in items)
            db.InventoryItems.Add(new InventoryItem { Id = Guid.NewGuid(), UserId = i.UserId, ItemId = i.ItemId });

        db.RunInventoryItems.RemoveRange(items);
        await db.SaveChangesAsync();
        await NotifyAsync("inventory", "return-from-run", userId);
    }

    public async Task LoseRunItemsAsync(Guid userId)
    {
        await using var db = _dbFactory.CreateDbContext();

        var runItemIds = await db.RunInventoryItems
            .Where(x => x.UserId == userId)
            .Select(x => x.ItemId)
            .ToListAsync();

        var equipment = await db.Equipments.FirstAsync(x => x.UserId == userId);

        var equippedItemIds = new[]
        {
            equipment.HeadId, equipment.BodyId, equipment.GlovesId,
            equipment.LegsId, equipment.BootsId, equipment.WeaponSlot1Id,
            equipment.WeaponSlot2Id, equipment.WeaponSlot3Id, equipment.WeaponSlot4Id
        }.Where(x => x.HasValue).Select(x => x!.Value);

        equipment.HeadId = equipment.BodyId = equipment.GlovesId =
        equipment.LegsId = equipment.BootsId = equipment.WeaponSlot1Id =
        equipment.WeaponSlot2Id = equipment.WeaponSlot3Id = equipment.WeaponSlot4Id = null;

        var allItemIds = runItemIds.Concat(equippedItemIds).Distinct().ToList();

        db.RunInventoryItems.RemoveRange(db.RunInventoryItems.Where(x => x.UserId == userId));

        var weapons = await db.Weapons.Where(x => allItemIds.Contains(x.Id)).ToListAsync();
        var armors = await db.Armors.Where(x => allItemIds.Contains(x.Id)).ToListAsync();

        db.Weapons.RemoveRange(weapons);
        db.Armors.RemoveRange(armors);

        await db.SaveChangesAsync();
        await NotifyAsync("inventory", "lose-run-items", userId);
    }

    public async Task MoveToMarketAsync(Guid userId, Guid itemId)
    {
        await using var db = _dbFactory.CreateDbContext();

        var item = await db.InventoryItems
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId)
            ?? throw new InvalidOperationException("Item not in inventory");

        db.MarketInventoryItems.Add(new MarketInventoryItem { Id = Guid.NewGuid(), UserId = userId, ItemId = itemId });
        db.InventoryItems.Remove(item);
        await db.SaveChangesAsync();
        await NotifyAsync("inventory", "move-to-market", userId);
    }

    public async Task ReturnFromMarketAsync(Guid userId, Guid itemId)
    {
        await using var db = _dbFactory.CreateDbContext();

        var item = await db.MarketInventoryItems
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId)
            ?? throw new InvalidOperationException();

        db.InventoryItems.Add(new InventoryItem { Id = Guid.NewGuid(), UserId = userId, ItemId = itemId });
        db.MarketInventoryItems.Remove(item);
        await db.SaveChangesAsync();
        await NotifyAsync("inventory", "return-from-market", userId);
    }

    public async Task TransferFromSellerToBuyerAsync(Guid sellerId, Guid buyerId, Guid itemId)
    {
        await using var db = _dbFactory.CreateDbContext();

        var listing = await db.MarketInventoryItems
            .FirstOrDefaultAsync(x => x.UserId == sellerId && x.ItemId == itemId)
            ?? throw new InvalidOperationException();

        db.MarketInventoryItems.Remove(listing);
        db.InventoryItems.Add(new InventoryItem { Id = Guid.NewGuid(), UserId = buyerId, ItemId = itemId });
        await db.SaveChangesAsync();
        await NotifyAsync("inventory", "transfer-item", buyerId, new { sellerId, itemId });
    }

    public async Task<ItemCollectionDTO> GetInventoryAsync(Guid userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var ids = await db.InventoryItems.Where(x => x.UserId == userId).Select(x => x.ItemId).ToListAsync();
        return await LoadItems(db, ids);
    }

    public async Task<ItemCollectionDTO> GetRunInventoryAsync(Guid userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var ids = await db.RunInventoryItems.Where(x => x.UserId == userId).Select(x => x.ItemId).ToListAsync();
        return await LoadItems(db, ids);
    }

    public async Task<ItemCollectionDTO> GetMarketInventoryAsync(Guid userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var ids = await db.MarketInventoryItems.Where(x => x.UserId == userId).Select(x => x.ItemId).ToListAsync();
        return await LoadItems(db, ids);
    }

    public async Task<ItemCollectionDTO> GetItemsAsync(Guid userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var ids = await db.InventoryItems.Where(x => x.UserId == userId).Select(x => x.ItemId).ToListAsync();
        return await LoadItems(db, ids);
    }

    public async Task AddToInventoryAsync(Guid userId, Guid itemId)
    {
        await using var db = _dbFactory.CreateDbContext();
        db.InventoryItems.Add(new InventoryItem { Id = Guid.NewGuid(), UserId = userId, ItemId = itemId });
        await db.SaveChangesAsync();
        await NotifyAsync("inventory", "add-inventory-item", userId, new { itemId });
    }

    public async Task AddToRunInventoryAsync(Guid userId, Guid itemId)
    {
        await using var db = _dbFactory.CreateDbContext();
        db.RunInventoryItems.Add(new RunInventoryItem { Id = Guid.NewGuid(), UserId = userId, ItemId = itemId });
        await db.SaveChangesAsync();
        await NotifyAsync("inventory", "add-run-item", userId, new { itemId });
    }

    private static async Task<ItemCollectionDTO> LoadItems(AppDbContext db, List<Guid> ids)
    {
        var weapons = await db.Weapons.Where(x => ids.Contains(x.Id)).Include(x => x.Elements).ToListAsync();
        var armors = await db.Armors.Where(x => ids.Contains(x.Id)).Include(x => x.Elements).ToListAsync();

        return new ItemCollectionDTO
        {
            Weapons = weapons.Select(MapWeapon).ToList(),
            Armors = armors.Select(MapArmor).ToList()
        };
    }

    private static WeaponDTO MapWeapon(Weapon x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Category = x.Category,
        WeaponType = x.WeaponType,
        Cut = x.Cut,
        Blunt = x.Blunt,
        Elements = x.Elements.Select(e => new ItemElementDTO { Type = e.ItemElementType, Value = e.Value }).ToList()
    };

    private static ArmorDTO MapArmor(Armor x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Category = x.Category,
        ArmorType = x.ArmorType,
        CutResistance = x.CutResistance,
        BluntResistance = x.BluntResistance,
        Elements = x.Elements.Select(e => new ItemElementDTO { Type = e.ItemElementType, Value = e.Value }).ToList()
    };

    private Task NotifyAsync(string domain, string action, Guid userId, object? data = null)
        => _realtimeNotifier?.AppChangedAsync(domain, action, userId, data) ?? Task.CompletedTask;
}
