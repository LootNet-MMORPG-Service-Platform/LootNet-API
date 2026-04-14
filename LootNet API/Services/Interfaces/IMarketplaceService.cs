using LootNet_API.DTO;
using LootNet_API.Enums;
using LootNet_API.Models.Market;

namespace LootNet_API.Services.Interfaces;

public interface IMarketplaceService
{
    Task<PagedResultDTO<WeaponMarketDTO>> GetWeaponsAsync(WeaponQueryDTO query);
    Task<PagedResultDTO<ArmorMarketDTO>> GetArmorsAsync(ArmorQueryDTO query);
    Task<MarketListing> CreateListingAsync(Guid userId, CreateMarketListingDTO dto);
    Task BuyItemAsync(Guid userId, Guid listingId);
}
