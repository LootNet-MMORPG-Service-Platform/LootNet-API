using LootNet_API.DTO;
using LootNet_API.Enums;
using LootNet_API.Models.Market;

namespace LootNet_API.Services.Interfaces;

public interface IMarketplaceService
{
    Task<PagedResultDTO<WeaponMarketDTO>> GetWeaponsAsync(Guid userId, WeaponQueryDTO query);
    Task<PagedResultDTO<ArmorMarketDTO>> GetArmorsAsync(Guid userId, ArmorQueryDTO query);
    Task<List<MyMarketListingDTO>> GetMyListingsAsync(Guid userId);
    Task<List<MarketTransactionDTO>> GetMyTransactionsAsync(Guid userId);
    Task<MarketListing> CreateListingAsync(Guid userId, CreateMarketListingDTO dto);
    Task BuyItemAsync(Guid userId, Guid listingId);
}
