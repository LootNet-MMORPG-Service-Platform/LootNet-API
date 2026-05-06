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
    private readonly IRealtimeNotifier? _realtimeNotifier;

    public MarketplaceService(AppDbContext context, IInventoryService inventoryService, IRealtimeNotifier? realtimeNotifier = null)
    {
        _context = context;
        _inventoryService = inventoryService;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<PagedResultDTO<WeaponMarketDTO>> GetWeaponsAsync(Guid userId, WeaponQueryDTO q)
    {
        if (!q.Price?.IsValid ?? false) throw new InvalidOperationException("Invalid price range");
        if (!q.Cut?.IsValid ?? false) throw new InvalidOperationException("Invalid cut range");
        if (!q.Blunt?.IsValid ?? false) throw new InvalidOperationException("Invalid blunt range");

        var listings = await _context.MarketListings
            .Where(x => !x.IsSold && x.Category == ItemCategory.Weapon && x.SellerId != userId)
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
        var sellerIds = filtered.Select(x => x.Listing.SellerId).Distinct().ToList();
        var sellers = await _context.Users.Where(x => sellerIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Username, x.ProfileImagePath })
            .ToListAsync();

        var total = filtered.Count;

        var projected = filtered
            .Select(x =>
            {
                var seller = sellers.FirstOrDefault(s => s.Id == x.Listing.SellerId);
                return MapWeapon(x.Listing, x.Weapon, seller?.Username, seller?.ProfileImagePath);
            })
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

    public async Task<PagedResultDTO<ArmorMarketDTO>> GetArmorsAsync(Guid userId, ArmorQueryDTO q)
    {
        if (!q.Price?.IsValid ?? false) throw new InvalidOperationException("Invalid price range");
        if (!q.CutResistance?.IsValid ?? false) throw new InvalidOperationException("Invalid cut range");
        if (!q.BluntResistance?.IsValid ?? false) throw new InvalidOperationException("Invalid blunt range");

        var listings = await _context.MarketListings
            .Where(x => !x.IsSold && x.Category == ItemCategory.Armor && x.SellerId != userId)
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
        var sellerIds = filtered.Select(x => x.Listing.SellerId).Distinct().ToList();
        var sellers = await _context.Users.Where(x => sellerIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Username, x.ProfileImagePath })
            .ToListAsync();

        var total = filtered.Count;

        var projected = filtered
            .Select(x =>
            {
                var seller = sellers.FirstOrDefault(s => s.Id == x.Listing.SellerId);
                return MapArmor(x.Listing, x.Armor, seller?.Username, seller?.ProfileImagePath);
            })
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

    public async Task<PagedResultDTO<MyMarketListingDTO>> GetMyListingsAsync(Guid userId, MyListingsQueryDTO query)
    {
        var listings = await _context.MarketListings
            .Where(x => !x.IsSold && x.SellerId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            listings = listings.Where(x => x.ItemId != Guid.Empty).ToList();
            var searchLower = search.ToLower();
            var weaponIds = await _context.Weapons.Where(x => x.Name.ToLower().Contains(searchLower)).Select(x => x.Id).ToListAsync();
            var armorIds = await _context.Armors.Where(x => x.Name.ToLower().Contains(searchLower)).Select(x => x.Id).ToListAsync();
            var ids = weaponIds.Concat(armorIds).ToHashSet();
            listings = listings.Where(x => ids.Contains(x.ItemId)).ToList();
        }

        if (query.Category.HasValue)
            listings = listings.Where(x => x.Category == query.Category.Value).ToList();

        if (query.Price is not null)
            listings = listings.Where(x => PassRange(query.Price, x.Price)).ToList();

        var itemIds = listings.Select(x => x.ItemId).ToList();

        var weapons = await _context.Weapons
            .Include(x => x.Elements)
            .Where(x => itemIds.Contains(x.Id))
            .ToListAsync();

        var armors = await _context.Armors
            .Include(x => x.Elements)
            .Where(x => itemIds.Contains(x.Id))
            .ToListAsync();

        var mapped = listings.Select(l =>
        {
            var weapon = weapons.FirstOrDefault(w => w.Id == l.ItemId);
            if (weapon != null)
            {
                return new MyMarketListingDTO
                {
                    ListingId = l.Id,
                    ItemId = weapon.Id,
                    Name = weapon.Name,
                    Price = l.Price,
                    Category = ItemCategory.Weapon,
                    CreatedAt = l.CreatedAt,
                    WeaponType = weapon.WeaponType,
                    Cut = weapon.Cut,
                    Blunt = weapon.Blunt,
                    Elements = weapon.Elements.Select(e => new ItemElementDTO
                    {
                        Type = e.ItemElementType,
                        Value = e.Value
                    }).ToList()
                };
            }

            var armor = armors.FirstOrDefault(a => a.Id == l.ItemId);
            if (armor != null)
            {
                return new MyMarketListingDTO
                {
                    ListingId = l.Id,
                    ItemId = armor.Id,
                    Name = armor.Name,
                    Price = l.Price,
                    Category = ItemCategory.Armor,
                    CreatedAt = l.CreatedAt,
                    ArmorType = armor.ArmorType,
                    CutResistance = armor.CutResistance,
                    BluntResistance = armor.BluntResistance,
                    Elements = armor.Elements.Select(e => new ItemElementDTO
                    {
                        Type = e.ItemElementType,
                        Value = e.Value
                    }).ToList()
                };
            }

            return new MyMarketListingDTO
            {
                ListingId = l.Id,
                ItemId = l.ItemId,
                Price = l.Price,
                Category = l.Category,
                CreatedAt = l.CreatedAt,
                Name = "Unknown Item"
            };
        }).ToList();

        var paged = mapped
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedResultDTO<MyMarketListingDTO>
        {
            Items = paged,
            TotalCount = mapped.Count,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<PagedResultDTO<MyMarketListingDTO>> GetListingsBySellerAsync(Guid sellerId, MyListingsQueryDTO query)
    {
        var listings = await _context.MarketListings
            .Where(x => !x.IsSold && x.SellerId == sellerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.Trim().ToLower();
            var weaponIds = await _context.Weapons.Where(x => x.Name.ToLower().Contains(searchLower)).Select(x => x.Id).ToListAsync();
            var armorIds = await _context.Armors.Where(x => x.Name.ToLower().Contains(searchLower)).Select(x => x.Id).ToListAsync();
            var ids = weaponIds.Concat(armorIds).ToHashSet();
            listings = listings.Where(x => ids.Contains(x.ItemId)).ToList();
        }

        if (query.Category.HasValue)
            listings = listings.Where(x => x.Category == query.Category.Value).ToList();

        if (query.Price is not null)
            listings = listings.Where(x => PassRange(query.Price, x.Price)).ToList();

        var itemIds = listings.Select(x => x.ItemId).ToList();
        var weapons = await _context.Weapons.Include(x => x.Elements).Where(x => itemIds.Contains(x.Id)).ToListAsync();
        var armors = await _context.Armors.Include(x => x.Elements).Where(x => itemIds.Contains(x.Id)).ToListAsync();

        var mapped = listings.Select(l =>
        {
            var weapon = weapons.FirstOrDefault(w => w.Id == l.ItemId);
            if (weapon != null)
            {
                return new MyMarketListingDTO
                {
                    ListingId = l.Id, ItemId = weapon.Id, Name = weapon.Name, Price = l.Price, Category = ItemCategory.Weapon, CreatedAt = l.CreatedAt,
                    WeaponType = weapon.WeaponType, Cut = weapon.Cut, Blunt = weapon.Blunt,
                    Elements = weapon.Elements.Select(e => new ItemElementDTO { Type = e.ItemElementType, Value = e.Value }).ToList()
                };
            }

            var armor = armors.FirstOrDefault(a => a.Id == l.ItemId);
            if (armor != null)
            {
                return new MyMarketListingDTO
                {
                    ListingId = l.Id, ItemId = armor.Id, Name = armor.Name, Price = l.Price, Category = ItemCategory.Armor, CreatedAt = l.CreatedAt,
                    ArmorType = armor.ArmorType, CutResistance = armor.CutResistance, BluntResistance = armor.BluntResistance,
                    Elements = armor.Elements.Select(e => new ItemElementDTO { Type = e.ItemElementType, Value = e.Value }).ToList()
                };
            }

            return new MyMarketListingDTO { ListingId = l.Id, ItemId = l.ItemId, Price = l.Price, Category = l.Category, CreatedAt = l.CreatedAt, Name = "Unknown Item" };
        }).ToList();

        var paged = mapped.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToList();
        return new PagedResultDTO<MyMarketListingDTO> { Items = paged, TotalCount = mapped.Count, PageNumber = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<MyListingsSummaryDTO> GetMyListingsSummaryAsync(Guid userId)
    {
        var listings = await _context.MarketListings
            .Where(x => !x.IsSold && x.SellerId == userId)
            .ToListAsync();

        return new MyListingsSummaryDTO
        {
            TotalItemsListed = listings.Count,
            TotalListedValue = listings.Sum(x => x.Price)
        };
    }

    public async Task<PagedResultDTO<MarketTransactionDTO>> GetMyTransactionsAsync(Guid userId, MarketTransactionsQueryDTO query)
    {
        var transactions = await _context.Transactions
            .Where(x => x.BuyerId == userId || x.SellerId == userId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync();

        var userIds = transactions
            .SelectMany(x => new[] { x.BuyerId, x.SellerId })
            .Distinct()
            .ToList();

        var users = await _context.Users
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Username })
            .ToListAsync();

        var itemIds = transactions.Select(x => x.ItemId).Distinct().ToList();
        var weaponNames = await _context.Weapons
            .Where(x => itemIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();
        var armorNames = await _context.Armors
            .Where(x => itemIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync();

        var mapped = transactions.Select(t =>
        {
            var isSale = t.SellerId == userId;
            var counterpartyId = isSale ? t.BuyerId : t.SellerId;
            var counterpartyName = users.FirstOrDefault(x => x.Id == counterpartyId)?.Username ?? "Unknown";
            var itemName = weaponNames.FirstOrDefault(x => x.Id == t.ItemId)?.Name
                           ?? armorNames.FirstOrDefault(x => x.Id == t.ItemId)?.Name
                           ?? "Unknown Item";

            return new MarketTransactionDTO
            {
                TransactionId = t.Id,
                ItemId = t.ItemId,
                ItemName = itemName,
                Price = t.Price,
                Timestamp = t.Timestamp,
                IsSale = isSale,
                CounterpartyUsername = counterpartyName,
                CounterpartyUserId = counterpartyId
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            mapped = mapped.Where(x =>
                    x.ItemName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.CounterpartyUsername.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (query.IsSale.HasValue)
            mapped = mapped.Where(x => x.IsSale == query.IsSale.Value).ToList();

        if (query.From.HasValue)
            mapped = mapped.Where(x => x.Timestamp >= query.From.Value).ToList();

        if (query.To.HasValue)
            mapped = mapped.Where(x => x.Timestamp <= query.To.Value).ToList();

        if (query.Price is not null)
            mapped = mapped.Where(x => PassRange(query.Price, x.Price)).ToList();

        var paged = mapped
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedResultDTO<MarketTransactionDTO>
        {
            Items = paged,
            TotalCount = mapped.Count,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<MarketTransactionsSummaryDTO> GetMyTransactionsSummaryAsync(Guid userId)
    {
        var transactions = await _context.Transactions
            .Where(x => x.BuyerId == userId || x.SellerId == userId)
            .ToListAsync();

        var totalSold = transactions.Where(x => x.SellerId == userId).Sum(x => (decimal)x.Price);
        var totalBought = transactions.Where(x => x.BuyerId == userId).Sum(x => (decimal)x.Price);

        return new MarketTransactionsSummaryDTO
        {
            TotalSold = totalSold,
            TotalBought = totalBought
        };
    }

    public async Task ChangeListingPriceAsync(Guid userId, Guid listingId, decimal price)
    {
        if (price <= 0)
            throw new InvalidOperationException("Price must be greater than 0.");

        var listing = await _context.MarketListings.FirstOrDefaultAsync(x => x.Id == listingId && !x.IsSold);
        if (listing == null)
            throw new InvalidOperationException("Listing not found.");
        if (listing.SellerId != userId)
            throw new InvalidOperationException("You can update only your own listing.");

        listing.Price = price;
        await _context.SaveChangesAsync();
        await NotifyAsync("market", "listing-price-changed", userId, new { listingId, price });
    }

    public async Task CancelListingAsync(Guid userId, Guid listingId)
    {
        var listing = await _context.MarketListings.FirstOrDefaultAsync(x => x.Id == listingId && !x.IsSold);
        if (listing == null)
            throw new InvalidOperationException("Listing not found.");
        if (listing.SellerId != userId)
            throw new InvalidOperationException("You can cancel only your own listing.");

        await _inventoryService.ReturnFromMarketAsync(userId, listing.ItemId);
        _context.MarketListings.Remove(listing);
        await _context.SaveChangesAsync();
        await NotifyAsync("market", "listing-cancelled", userId, new { listingId });
    }

    public async Task<MarketListing> CreateListingAsync(Guid userId, CreateMarketListingDTO dto)
    {
        var item = await _context.Weapons.FirstOrDefaultAsync(x => x.Id == dto.ItemId)
                 ?? (Item?)await _context.Armors.FirstOrDefaultAsync(x => x.Id == dto.ItemId);

        if (item == null)
            throw new InvalidOperationException("Item not found");

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
        await NotifyAsync("market", "listing-created", userId, new { listingId = listing.Id, dto.ItemId });

        return listing;
    }

    public async Task BuyItemAsync(Guid buyerId, Guid listingId)
    {
        var listing = await _context.MarketListings
            .FirstOrDefaultAsync(x => x.Id == listingId && !x.IsSold);

        if (listing == null)
            throw new InvalidOperationException("Listing not found");

        var buyer = await _context.Users.FirstOrDefaultAsync(x => x.Id == buyerId);
        if (buyer == null)
            throw new InvalidOperationException("Buyer account not found. Log in again.");

        var seller = await _context.Users.FirstOrDefaultAsync(x => x.Id == listing.SellerId);
        if (seller == null)
            throw new InvalidOperationException("Seller account not found.");

        if (buyer.Id == seller.Id)
            throw new InvalidOperationException("You cannot buy your own listing.");

        if (buyer.Currency < listing.Price)
            throw new InvalidOperationException("Not enough currency");

        buyer.Currency -= listing.Price;
        seller.Currency += listing.Price;

        await _inventoryService.TransferFromSellerToBuyerAsync(seller.Id, buyerId, listing.ItemId);

        listing.IsSold = true;

        await _context.SaveChangesAsync();
        await NotifyAsync("market", "listing-sold", buyerId, new { listingId, sellerId = seller.Id, listing.ItemId });
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
    private WeaponMarketDTO MapWeapon(MarketListing l, Weapon w, string? sellerUsername, string? sellerProfileImagePath)
    {
        return new WeaponMarketDTO
        {
            ListingId = l.Id,
            ItemId = w.Id,
            SellerId = l.SellerId,
            Name = w.Name,
            Price = l.Price,
            WeaponType = w.WeaponType,
            Cut = w.Cut,
            Blunt = w.Blunt,
            Elements = w.Elements.Select(e => new ItemElementDTO
            {
                Type = e.ItemElementType,
                Value = e.Value
            }).ToList(),
            SellerUsername = sellerUsername ?? "Unknown",
            SellerProfileImagePath = sellerProfileImagePath
        };
    }
    private ArmorMarketDTO MapArmor(MarketListing l, Armor a, string? sellerUsername, string? sellerProfileImagePath)
    {
        return new ArmorMarketDTO
        {
            ListingId = l.Id,
            ItemId = a.Id,
            SellerId = l.SellerId,
            Name = a.Name,
            Price = l.Price,
            ArmorType = a.ArmorType,
            CutResistance = a.CutResistance,
            BluntResistance = a.BluntResistance,
            Elements = a.Elements.Select(e => new ItemElementDTO
            {
                Type = e.ItemElementType,
                Value = e.Value
            }).ToList(),
            SellerUsername = sellerUsername ?? "Unknown",
            SellerProfileImagePath = sellerProfileImagePath
        };
    }

    private Task NotifyAsync(string domain, string action, Guid userId, object? data = null)
        => _realtimeNotifier?.AppChangedAsync(domain, action, userId, data) ?? Task.CompletedTask;
}
