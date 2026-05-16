using LootNet_API.DTO;
using LootNet_API.DTO.Market;
using LootNet_API.Enums;
using LootNet_API.Models.Market;

namespace LootNet_API.Services.Interfaces;

public interface IMarketplaceService
{
    Task<PagedResultDTO<WeaponMarketDTO>> GetWeaponsAsync(Guid userId, WeaponQueryDTO query);
    Task<PagedResultDTO<ArmorMarketDTO>> GetArmorsAsync(Guid userId, ArmorQueryDTO query);
    Task<PagedResultDTO<MyMarketListingDTO>> GetMyListingsAsync(Guid userId, MyListingsQueryDTO query);
    Task<PagedResultDTO<MyMarketListingDTO>> GetListingsBySellerAsync(Guid sellerId, MyListingsQueryDTO query);
    Task<MyListingsSummaryDTO> GetMyListingsSummaryAsync(Guid userId);
    Task<PagedResultDTO<MarketTransactionDTO>> GetMyTransactionsAsync(Guid userId, MarketTransactionsQueryDTO query);
    Task<MarketTransactionsSummaryDTO> GetMyTransactionsSummaryAsync(Guid userId);
    Task<PagedResultDTO<SellInventoryItemDTO>> GetSellInventoryAsync(Guid userId, SellInventoryQueryDTO query);
    MarketEconomyDTO GetEconomy();
    MarketSaleTaxDTO CalculateSaleTax(decimal grossPrice);
    Task<BotSaleOfferDTO> GetBotSaleOfferAsync(Guid userId, Guid itemId);
    Task<BotSaleResultDTO> SellItemToBotAsync(Guid userId, Guid itemId);
    Task<WebDailyRewardDTO> ClaimWebDailyAsync(Guid userId);
    Task ChangeListingPriceAsync(Guid userId, Guid listingId, decimal price);
    Task CancelListingAsync(Guid userId, Guid listingId);
    Task<MarketListing> CreateListingAsync(Guid userId, CreateMarketListingDTO dto);
    Task BuyItemAsync(Guid userId, Guid listingId);
}
