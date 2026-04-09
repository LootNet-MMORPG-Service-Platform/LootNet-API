namespace LootNet_API.Services;

using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.Enums;
using LootNet_API.Models.Items;
using LootNet_API.Models.Market;
using Microsoft.EntityFrameworkCore;

public class MarketplaceService : IMarketplaceService
{
    private readonly AppDbContext _context;

    public MarketplaceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MarketListingDTO>> GetListingsAsync(ItemCategory? category = null,int pageNumber = 1,int pageSize = 20,string sort = "asc")
    {
        var query = _context.MarketListings
            .Where(x => !x.IsSold)
            .AsQueryable();

        if (category.HasValue)
            query = query.Where(x => x.Category == category.Value);

        query = sort.ToLower() == "desc"
            ? query.OrderByDescending(x => x.Price)
            : query.OrderBy(x => x.Price);

        query = query.Skip((pageNumber - 1) * pageSize)
                     .Take(pageSize);

        var listings = await query.ToListAsync();
        var result = new List<MarketListingDTO>();

        foreach (var listing in listings)
        {
            Item? item = await _context.Weapons.FirstOrDefaultAsync(w => w.Id == listing.ItemId)
                         as Item
                         ?? await _context.Armors.FirstOrDefaultAsync(a => a.Id == listing.ItemId);

            result.Add(new MarketListingDTO
            {
                Id = listing.Id,
                ItemName = item?.Name ?? "Unknown",
                Price = listing.Price,
                SellerId = listing.SellerId,
                Category = listing.Category
            });
        }

        return result;
    }

    public async Task<MarketListing> CreateListingAsync(Guid userId, CreateMarketListingDTO dto)
    {
        Item? item = await _context.Weapons.FirstOrDefaultAsync(w => w.Id == dto.ItemId && w.OwnerId == userId)
                     as Item
                     ?? await _context.Armors.FirstOrDefaultAsync(a => a.Id == dto.ItemId && a.OwnerId == userId);

        if (item == null) throw new InvalidOperationException("Item not found or not owned by user");

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
        var listing = await _context.MarketListings.FirstOrDefaultAsync(x => x.Id == listingId && !x.IsSold);
        if (listing == null) throw new InvalidOperationException("Listing not found");

        var buyer = await _context.Users.FirstAsync(u => u.Id == buyerId);
        if (buyer.Currency < listing.Price) throw new InvalidOperationException("Not enough currency");

        var seller = await _context.Users.FirstAsync(u => u.Id == listing.SellerId);

        buyer.Currency -= listing.Price;
        seller.Currency += listing.Price;

        Item? item = await _context.Weapons.FirstOrDefaultAsync(w => w.Id == listing.ItemId)
                     as Item
                     ?? await _context.Armors.FirstOrDefaultAsync(a => a.Id == listing.ItemId);

        if (item == null) throw new InvalidOperationException("Item not found");
        item.OwnerId = buyerId;

        listing.IsSold = true;

        await _context.SaveChangesAsync();
    }
}
