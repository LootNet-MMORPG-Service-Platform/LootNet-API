using LootNet_API.DTO;
using LootNet_API.Enums;
using LootNet_API.Models.Market;

namespace LootNet_API.Services;

public interface IMarketplaceService
{
    Task<List<MarketListingDTO>> GetListingsAsync(ItemCategory? category, int pageNumber, int pageSize, string sort);
    Task<MarketListing> CreateListingAsync(Guid userId, CreateMarketListingDTO dto);
    Task BuyItemAsync(Guid userId, Guid listingId);
}
