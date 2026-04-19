namespace LootNet_API.Services;

using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.DTO.Items;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Models.Market;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class MarketplaceService : IMarketplaceService
{
    private readonly AppDbContext _context;
    private readonly IInventoryService _inventoryService;

    public MarketplaceService(AppDbContext context, IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task<PagedResultDTO<WeaponMarketDTO>> GetWeaponsAsync(WeaponQueryDTO q)
    {
        if (!q.Price?.IsValid ?? false) throw new InvalidOperationException("Invalid price range");
        if (!q.Cut?.IsValid ?? false) throw new InvalidOperationException("Invalid cut range");
        if (!q.Blunt?.IsValid ?? false) throw new InvalidOperationException("Invalid blunt range");

        var listings = await _context.MarketListings
            .Where(x => !x.IsSold && x.Category == ItemCategory.Weapon)
            .ToListAsync();

        var weapons = await _context.Weapons
            .Include(x => x.Elements)
            .Where(x => listings.Select(l => l.ItemId).Contains(x.Id))
            .ToListAsync();

        var joined = listings
            .Join(weapons,
                l => l.ItemId,
                w => w.Id,
                (l, w) => new { Listing = l, Weapon = w })
            .ToList();

        var filtered = joined
            .Where(x => PassWeaponFilters(q, x.Listing, x.Weapon))
            .ToList();

        var total = filtered.Count;

        var projected = filtered
            .Select(x => MapWeapon(x.Listing, x.Weapon))
            .ToList();

        var sorted = ApplyWeaponSort(projected, q);

        var paged = sorted
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToList();

        return new PagedResultDTO<WeaponMarketDTO>
        {
            Items = paged,
            TotalCount = total,
            PageNumber = q.PageNumber,
            PageSize = q.PageSize
        };
    }

    public async Task<PagedResultDTO<ArmorMarketDTO>> GetArmorsAsync(ArmorQueryDTO q)
    {
        if (!q.Price?.IsValid ?? false) throw new InvalidOperationException("Invalid price range");
        if (!q.CutResistance?.IsValid ?? false) throw new InvalidOperationException("Invalid cut range");
        if (!q.BluntResistance?.IsValid ?? false) throw new InvalidOperationException("Invalid blunt range");

        var listings = await _context.MarketListings
            .Where(x => !x.IsSold && x.Category == ItemCategory.Armor)
            .ToListAsync();

        var armors = await _context.Armors
            .Include(x => x.Elements)
            .Where(x => listings.Select(l => l.ItemId).Contains(x.Id))
            .ToListAsync();

        var joined = listings
            .Join(armors,
                l => l.ItemId,
                a => a.Id,
                (l, a) => new { Listing = l, Armor = a })
            .ToList();

        var filtered = joined
            .Where(x => PassArmorFilters(q, x.Listing, x.Armor))
            .ToList();

        var total = filtered.Count;

        var projected = filtered
            .Select(x => MapArmor(x.Listing, x.Armor))
            .ToList();

        var sorted = ApplyArmorSort(projected, q);

        var paged = sorted
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToList();

        return new PagedResultDTO<ArmorMarketDTO>
        {
            Items = paged,
            TotalCount = total,
            PageNumber = q.PageNumber,
            PageSize = q.PageSize
        };
    }

    public async Task<MarketListing> CreateListingAsync(Guid userId, CreateMarketListingDTO dto)
    {
        var item = await _context.Weapons.FirstOrDefaultAsync(x => x.Id == dto.ItemId)
                 ?? (Item?)await _context.Armors.FirstOrDefaultAsync(x => x.Id == dto.ItemId);

        if (item == null)
            throw new InvalidOperationException("Item not found");

        await _inventoryService.MoveToMarketAsync(userId, dto.ItemId);

        var listing = new MarketListing
        {
            Id = Guid.NewGuid(),
            SellerId = userId,
            ItemId = dto.ItemId,
            Price = dto.Price,
            Category = item.Category
        };

        _context.MarketListings.Add(listing);

        await _context.SaveChangesAsync();

        return listing;
    }

    public async Task BuyItemAsync(Guid buyerId, Guid listingId)
    {
        var listing = await _context.MarketListings
            .FirstOrDefaultAsync(x => x.Id == listingId && !x.IsSold);

        if (listing == null)
            throw new InvalidOperationException("Listing not found");

        var buyer = await _context.Users.FirstAsync(x => x.Id == buyerId);
        var seller = await _context.Users.FirstAsync(x => x.Id == listing.SellerId);

        if (buyer.Currency < listing.Price)
            throw new InvalidOperationException("Not enough currency");

        buyer.Currency -= listing.Price;
        seller.Currency += listing.Price;

        await _inventoryService.TransferFromSellerToBuyerAsync(seller.Id, buyerId, listing.ItemId);

        listing.IsSold = true;

        await _context.SaveChangesAsync();
    }
    private bool PassWeaponFilters(WeaponQueryDTO q, MarketListing l, Weapon w)
    {
        if (!string.IsNullOrWhiteSpace(q.Search) &&
            !w.Name.Contains(q.Search, StringComparison.OrdinalIgnoreCase))
            return false;

        if (q.Types?.Any() == true && !q.Types.Contains(w.WeaponType))
            return false;

        if (q.Price != null && !PassRange(q.Price, l.Price))
            return false;

        if (q.Cut != null && !PassRange(q.Cut, w.Cut))
            return false;

        if (q.Blunt != null && !PassRange(q.Blunt, w.Blunt))
            return false;

        if (q.Elements?.Any() == true &&
            !w.Elements.Any(e => q.Elements.Contains(e.ItemElementType)))
            return false;

        return true;
    }
    private bool PassArmorFilters(ArmorQueryDTO q, MarketListing l, Armor a)
    {
        if (!string.IsNullOrWhiteSpace(q.Search) &&
            !a.Name.Contains(q.Search, StringComparison.OrdinalIgnoreCase))
            return false;

        if (q.Types?.Any() == true && !q.Types.Contains(a.ArmorType))
            return false;

        if (q.Price != null && !PassRange(q.Price, l.Price))
            return false;

        if (q.CutResistance != null && !PassRange(q.CutResistance, a.CutResistance))
            return false;

        if (q.BluntResistance != null && !PassRange(q.BluntResistance, a.BluntResistance))
            return false;

        if (q.Elements?.Any() == true &&
            !a.Elements.Any(e => q.Elements.Contains(e.ItemElementType)))
            return false;

        return true;
    }
    private bool PassRange<T>(RangeFilter<T> r, T value)
    where T : struct, IComparable<T>
    {
        if (r.Min.HasValue && value.CompareTo(r.Min.Value) < 0)
            return false;

        if (r.Max.HasValue && value.CompareTo(r.Max.Value) > 0)
            return false;

        return true;
    }
    private List<WeaponMarketDTO> ApplyWeaponSort(List<WeaponMarketDTO> list, WeaponQueryDTO q)
    {
        return q.SortColumn switch
        {
            WeaponSortColumns.Name =>
                q.SortDirection == SortDirection.Desc
                    ? list.OrderByDescending(x => x.Name).ToList()
                    : list.OrderBy(x => x.Name).ToList(),

            WeaponSortColumns.Price =>
                q.SortDirection == SortDirection.Desc
                    ? list.OrderByDescending(x => x.Price).ToList()
                    : list.OrderBy(x => x.Price).ToList(),

            WeaponSortColumns.Cut =>
                q.SortDirection == SortDirection.Desc
                    ? list.OrderByDescending(x => x.Cut).ToList()
                    : list.OrderBy(x => x.Cut).ToList(),

            WeaponSortColumns.Blunt =>
                q.SortDirection == SortDirection.Desc
                    ? list.OrderByDescending(x => x.Blunt).ToList()
                    : list.OrderBy(x => x.Blunt).ToList(),

            _ => list.OrderBy(x => x.Price).ToList()
        };
    }
    private List<ArmorMarketDTO> ApplyArmorSort(List<ArmorMarketDTO> list, ArmorQueryDTO q)
    {
        return q.SortColumn switch
        {
            ArmorSortColumns.Name =>
                q.SortDirection == SortDirection.Desc
                    ? list.OrderByDescending(x => x.Name).ToList()
                    : list.OrderBy(x => x.Name).ToList(),

            ArmorSortColumns.Price =>
                q.SortDirection == SortDirection.Desc
                    ? list.OrderByDescending(x => x.Price).ToList()
                    : list.OrderBy(x => x.Price).ToList(),

            ArmorSortColumns.CutResistance =>
                q.SortDirection == SortDirection.Desc
                    ? list.OrderByDescending(x => x.CutResistance).ToList()
                    : list.OrderBy(x => x.CutResistance).ToList(),

            ArmorSortColumns.BluntResistance =>
                q.SortDirection == SortDirection.Desc
                    ? list.OrderByDescending(x => x.BluntResistance).ToList()
                    : list.OrderBy(x => x.BluntResistance).ToList(),

            _ => list.OrderBy(x => x.Price).ToList()
        };
    }
    private WeaponMarketDTO MapWeapon(MarketListing l, Weapon w)
    {
        return new WeaponMarketDTO
        {
            ListingId = l.Id,
            ItemId = w.Id,
            Name = w.Name,
            Price = l.Price,
            WeaponType = w.WeaponType,
            Cut = w.Cut,
            Blunt = w.Blunt,
            Elements = w.Elements.Select(e => new ItemElementDTO
            {
                Type = e.ItemElementType,
                Value = e.Value
            }).ToList()
        };
    }
    private ArmorMarketDTO MapArmor(MarketListing l, Armor a)
    {
        return new ArmorMarketDTO
        {
            ListingId = l.Id,
            ItemId = a.Id,
            Name = a.Name,
            Price = l.Price,
            ArmorType = a.ArmorType,
            CutResistance = a.CutResistance,
            BluntResistance = a.BluntResistance,
            Elements = a.Elements.Select(e => new ItemElementDTO
            {
                Type = e.ItemElementType,
                Value = e.Value
            }).ToList()
        };
    }
}
