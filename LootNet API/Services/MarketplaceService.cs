namespace LootNet_API.Services;

using LootNet_API.Configuration;
using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.DTO.Admin;
using LootNet_API.DTO.Items;
using LootNet_API.DTO.Market;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Models.Logs;
using LootNet_API.Models.Market;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public class MarketplaceService : IMarketplaceService
{
    private static readonly Guid BotBuyerId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly AppDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly MarketplaceEconomyOptions _economyOptions;
    private readonly IRealtimeNotifier? _realtimeNotifier;
    private readonly IItemGenerationService? _itemGenerationService;

    public MarketplaceService(
        AppDbContext context,
        IInventoryService inventoryService,
        IOptions<MarketplaceEconomyOptions>? economyOptions = null,
        IRealtimeNotifier? realtimeNotifier = null,
        IItemGenerationService? itemGenerationService = null)
    {
        _context = context;
        _inventoryService = inventoryService;
        _economyOptions = economyOptions?.Value ?? new MarketplaceEconomyOptions();
        _realtimeNotifier = realtimeNotifier;
        _itemGenerationService = itemGenerationService;
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
                TaxAmount = t.TaxAmount,
                SellerPayout = t.SellerPayout > 0 ? t.SellerPayout : t.Price,
                Timestamp = t.Timestamp,
                IsSale = isSale,
                CounterpartyUsername = counterpartyId == BotBuyerId ? "LootNet Bot" : counterpartyName,
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

        var totalSold = transactions
            .Where(x => x.SellerId == userId)
            .Sum(x => x.SellerPayout > 0 ? x.SellerPayout : (decimal)x.Price);
        var totalBought = transactions.Where(x => x.BuyerId == userId).Sum(x => (decimal)x.Price);

        return new MarketTransactionsSummaryDTO
        {
            TotalSold = totalSold,
            TotalBought = totalBought
        };
    }

    public MarketEconomyDTO GetEconomy()
    {
        var settings = GetEconomySettings();
        return new MarketEconomyDTO
        {
            DailyCurrencyReward = settings.DailyCurrencyReward,
            BotBasePrice = settings.BotBasePrice,
            BotStatMultiplier = settings.BotStatMultiplier,
            BotElementMultiplier = settings.BotElementMultiplier,
            IsPlayerToPlayerTaxEnabled = settings.IsPlayerToPlayerTaxEnabled,
            IsPlayerToBotTaxEnabled = settings.IsPlayerToBotTaxEnabled,
            BotSaleFormula = $"{settings.BotBasePrice} + (primary stats + element values * {settings.BotElementMultiplier}) * {settings.BotStatMultiplier}, rounded to full currency.",
            ProgressiveTaxBrackets = settings.ProgressiveTaxBrackets.Select(x => new MarketTaxBracketDTO
            {
                From = x.From,
                To = x.To,
                Rate = x.Rate
            }).ToList()
        };
    }

    public MarketSaleTaxDTO CalculateSaleTax(decimal grossPrice)
        => CalculateSaleTax(grossPrice, GetEconomySettings().IsPlayerToPlayerTaxEnabled);

    private MarketSaleTaxDTO CalculateSaleTax(decimal grossPrice, bool isEnabled)
    {
        if (grossPrice <= 0)
            throw new InvalidOperationException("Price must be greater than 0.");

        var tax = isEnabled
            ? GetEconomySettings().ProgressiveTaxBrackets.Sum(x => CalculateBracketTax(grossPrice, x))
            : 0m;
        tax = Math.Round(tax, 2, MidpointRounding.AwayFromZero);

        return new MarketSaleTaxDTO
        {
            GrossPrice = grossPrice,
            TaxAmount = tax,
            SellerPayout = grossPrice - tax,
            EffectiveTaxRate = Math.Round(tax / grossPrice, 4, MidpointRounding.AwayFromZero)
        };
    }

    public async Task<BotSaleOfferDTO> GetBotSaleOfferAsync(Guid userId, Guid itemId)
    {
        await EnsureItemIsSellableFromInventoryAsync(userId, itemId);

        var item = await LoadItemAsync(itemId);
        if (item == null)
            throw new InvalidOperationException("Item not found.");

        return CreateBotSaleOffer(item);
    }

    public async Task<BotSaleResultDTO> SellItemToBotAsync(Guid userId, Guid itemId)
    {
        await EnsureItemIsSellableFromInventoryAsync(userId, itemId);

        var inventoryItem = await _context.InventoryItems
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId);
        if (inventoryItem == null)
            throw new InvalidOperationException("Item not in inventory.");

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
            throw new InvalidOperationException("User account not found. Log in again.");

        var item = await LoadItemAsync(itemId);
        if (item == null)
            throw new InvalidOperationException("Item not found.");

        var offer = CreateBotSaleOffer(item);
        user.Currency += offer.SellerPayout;
        _context.InventoryItems.Remove(inventoryItem);

        switch (item)
        {
            case Weapon weapon:
                _context.Weapons.Remove(weapon);
                break;
            case Armor armor:
                _context.Armors.Remove(armor);
                break;
        }

        _context.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            BuyerId = BotBuyerId,
            SellerId = userId,
            ItemId = itemId,
            Price = DecimalToTransactionPrice(offer.OfferedPrice),
            SellerPayout = offer.SellerPayout,
            TaxAmount = offer.TaxAmount,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await NotifyAsync("market", "item-sold-to-bot", userId, new { itemId, paidAmount = offer.SellerPayout, taxAmount = offer.TaxAmount });

        return new BotSaleResultDTO
        {
            ItemId = itemId,
            ItemName = offer.ItemName,
            Category = offer.Category,
            PaidAmount = offer.SellerPayout,
            CurrencyAfterSale = user.Currency
        };
    }

    public async Task<WebDailyRewardDTO> ClaimWebDailyAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
            throw new InvalidOperationException("User account not found. Log in again.");

        var claimedToday = await _context.AdminLogs.AnyAsync(x =>
            x.Action == "WEB_DAILY_CLAIMED" &&
            x.TargetUserId == userId.ToString() &&
            x.CreatedAt.Date == DateTime.UtcNow.Date);

        if (claimedToday)
            throw new InvalidOperationException("Web daily already claimed.");

        var economy = GetEconomySettings();
        user.Currency += economy.DailyCurrencyReward;
        _context.AdminLogs.Add(new AdminLog
        {
            Id = Guid.NewGuid(),
            AdminId = userId,
            Action = "WEB_DAILY_CLAIMED",
            TargetUserId = userId.ToString(),
            Data = System.Text.Json.JsonSerializer.Serialize(new { currencyReward = economy.DailyCurrencyReward })
        });

        await _context.SaveChangesAsync();
        await NotifyAsync("reward", "web-daily-claimed", userId, new { currencyReward = economy.DailyCurrencyReward });

        return new WebDailyRewardDTO { CurrencyReward = economy.DailyCurrencyReward, CurrencyAfterReward = user.Currency };
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
        if (dto.Price <= 0)
            throw new InvalidOperationException("Price must be greater than 0.");

        await EnsureItemIsSellableFromInventoryAsync(userId, dto.ItemId);

        var inventoryItem = await _context.InventoryItems
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == dto.ItemId);
        if (inventoryItem == null)
            throw new InvalidOperationException("Item not in inventory.");

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

        _context.InventoryItems.Remove(inventoryItem);
        _context.MarketInventoryItems.Add(new MarketInventoryItem { Id = Guid.NewGuid(), UserId = userId, ItemId = dto.ItemId });
        _context.MarketListings.Add(listing);

        await _context.SaveChangesAsync();
        await NotifyAsync("inventory", "move-to-market", userId, new { dto.ItemId });
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

        var tax = CalculateSaleTax(listing.Price, GetEconomySettings().IsPlayerToPlayerTaxEnabled);
        buyer.Currency -= listing.Price;
        seller.Currency += tax.SellerPayout;

        await _inventoryService.TransferFromSellerToBuyerAsync(seller.Id, buyerId, listing.ItemId);

        listing.IsSold = true;
        _context.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            BuyerId = buyerId,
            SellerId = seller.Id,
            ItemId = listing.ItemId,
            Price = DecimalToTransactionPrice(listing.Price),
            TaxAmount = tax.TaxAmount,
            SellerPayout = tax.SellerPayout,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await NotifyAsync("market", "listing-sold", buyerId, new { listingId, sellerId = seller.Id, listing.ItemId, tax.TaxAmount, tax.SellerPayout });
    }

    private async Task EnsureItemIsSellableFromInventoryAsync(Guid userId, Guid itemId)
    {
        var exists = await _context.InventoryItems.AnyAsync(x => x.UserId == userId && x.ItemId == itemId);
        if (!exists)
            throw new InvalidOperationException("Item not in inventory.");

        if (await IsEquippedAsync(userId, itemId))
            throw new InvalidOperationException("Unequip item before selling it.");
    }

    private async Task<bool> IsEquippedAsync(Guid userId, Guid itemId)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(x => x.UserId == userId);
        if (equipment == null)
            return false;

        return new[]
        {
            equipment.HeadId, equipment.BodyId, equipment.GlovesId, equipment.LegsId, equipment.BootsId,
            equipment.WeaponSlot1Id, equipment.WeaponSlot2Id, equipment.WeaponSlot3Id, equipment.WeaponSlot4Id
        }.Any(x => x == itemId);
    }

    private async Task<Item?> LoadItemAsync(Guid itemId)
    {
        return await _context.Weapons.Include(x => x.Elements).FirstOrDefaultAsync(x => x.Id == itemId)
               ?? (Item?)await _context.Armors.Include(x => x.Elements).FirstOrDefaultAsync(x => x.Id == itemId);
    }

    private BotSaleOfferDTO CreateBotSaleOffer(Item item)
    {
        var settings = GetEconomySettings();
        var score = item switch
        {
            Weapon weapon => (decimal)(weapon.Cut + weapon.Blunt) + CalculateElementScore(weapon.Elements),
            Armor armor => (decimal)(armor.CutResistance + armor.BluntResistance) + CalculateElementScore(armor.Elements),
            _ => 0m
        };
        var price = Math.Max(1m, Math.Round(settings.BotBasePrice + score * settings.BotStatMultiplier, 0, MidpointRounding.AwayFromZero));
        var tax = CalculateSaleTax(price, settings.IsPlayerToBotTaxEnabled);

        return new BotSaleOfferDTO
        {
            ItemId = item.Id,
            ItemName = item.Name,
            Category = item.Category,
            StatScore = Math.Round(score, 2, MidpointRounding.AwayFromZero),
            OfferedPrice = price,
            TaxAmount = tax.TaxAmount,
            SellerPayout = tax.SellerPayout
        };
    }

    private decimal CalculateElementScore(IEnumerable<ItemElement> elements)
        => elements.Sum(x => (decimal)x.Value * GetEconomySettings().BotElementMultiplier);

    private EconomySettings GetEconomySettings()
    {
        var serialized = _context.AdminLogs
            .Where(x => x.Action == "UPDATE_MARKETPLACE_ECONOMY" && x.Data != null)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Data)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(serialized))
        {
            var dto = System.Text.Json.JsonSerializer.Deserialize<UpdateMarketplaceEconomyDTO>(serialized);
            if (dto != null)
                return NormalizeSettings(new EconomySettings
                {
                    DailyCurrencyReward = dto.DailyCurrencyReward,
                    BotBasePrice = dto.BotBasePrice,
                    BotStatMultiplier = dto.BotStatMultiplier,
                    BotElementMultiplier = dto.BotElementMultiplier,
                    IsPlayerToPlayerTaxEnabled = dto.IsPlayerToPlayerTaxEnabled,
                    IsPlayerToBotTaxEnabled = dto.IsPlayerToBotTaxEnabled,
                    ProgressiveTaxBrackets = dto.ProgressiveTaxBrackets.Select(x => new EconomyTaxBracket { From = x.From, To = x.To, Rate = x.Rate }).ToList()
                });
        }

        return NormalizeSettings(new EconomySettings
        {
            DailyCurrencyReward = _economyOptions.DailyCurrencyReward,
            BotBasePrice = _economyOptions.BotBasePrice,
            BotStatMultiplier = _economyOptions.BotStatMultiplier,
            BotElementMultiplier = _economyOptions.BotElementMultiplier,
            IsPlayerToPlayerTaxEnabled = _economyOptions.IsPlayerToPlayerTaxEnabled,
            IsPlayerToBotTaxEnabled = _economyOptions.IsPlayerToBotTaxEnabled,
            ProgressiveTaxBrackets = _economyOptions.ProgressiveTaxBrackets.Select(x => new EconomyTaxBracket { From = x.From, To = x.To, Rate = x.Rate }).ToList()
        });
    }

    private static EconomySettings NormalizeSettings(EconomySettings settings)
    {
        var normalized = settings.ProgressiveTaxBrackets
            .Where(x => x.Rate >= 0 && (!x.To.HasValue || x.To.Value > x.From))
            .OrderBy(x => x.From)
            .ThenBy(x => x.To ?? decimal.MaxValue)
            .ThenBy(x => x.Rate)
            .Select(x => new EconomyTaxBracket
            {
                From = Math.Round(x.From, 2, MidpointRounding.AwayFromZero),
                To = x.To.HasValue ? Math.Round(x.To.Value, 2, MidpointRounding.AwayFromZero) : null,
                Rate = Math.Round(x.Rate, 6, MidpointRounding.AwayFromZero)
            })
            .GroupBy(x => new { x.From, x.To, x.Rate })
            .Select(x => x.First())
            .ToList();

        var nonOverlapping = new List<EconomyTaxBracket>();
        foreach (var bracket in normalized)
        {
            if (nonOverlapping.Count == 0)
            {
                nonOverlapping.Add(bracket);
                continue;
            }

            var prev = nonOverlapping[^1];
            if (!prev.To.HasValue || bracket.From < prev.To.Value)
                continue;

            nonOverlapping.Add(bracket);
        }

        settings.ProgressiveTaxBrackets = nonOverlapping;
        return settings;
    }

    private static decimal CalculateBracketTax(decimal grossPrice, EconomyTaxBracket bracket)
    {
        if (grossPrice <= bracket.From)
            return 0m;

        var upper = bracket.To ?? grossPrice;
        var taxable = Math.Min(grossPrice, upper) - bracket.From;
        return taxable * bracket.Rate;
    }

    private static int DecimalToTransactionPrice(decimal price)
        => (int)Math.Round(price, 0, MidpointRounding.AwayFromZero);

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

    private sealed class EconomySettings
    {
        public decimal DailyCurrencyReward { get; set; }
        public decimal BotBasePrice { get; set; }
        public decimal BotStatMultiplier { get; set; }
        public decimal BotElementMultiplier { get; set; }
        public bool IsPlayerToPlayerTaxEnabled { get; set; }
        public bool IsPlayerToBotTaxEnabled { get; set; }
        public List<EconomyTaxBracket> ProgressiveTaxBrackets { get; set; } = new();
    }

    private sealed class EconomyTaxBracket
    {
        public decimal From { get; set; }
        public decimal? To { get; set; }
        public decimal Rate { get; set; }
    }
}
