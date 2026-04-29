using LootNet_API.DTO.Items;
using LootNet_API.Models.Items;

namespace LootNet_API.Services.Interfaces;

public interface IInventoryService
{
    Task<ItemCollectionDTO> GetItemsAsync(Guid userId);
    Task<ItemCollectionDTO> GetInventoryAsync(Guid userId);
    Task<ItemCollectionDTO> GetRunInventoryAsync(Guid userId);
    Task<ItemCollectionDTO> GetMarketInventoryAsync(Guid userId);

    Task MoveToRunAsync(Guid userId, List<Guid> itemIds);
    Task ReturnFromRunAsync(Guid userId);
    Task MoveToMarketAsync(Guid userId, Guid itemId);
    Task ReturnFromMarketAsync(Guid userId, Guid itemId);

    Task AddToInventoryAsync(Guid userId, Guid itemId);
    Task AddToRunInventoryAsync(Guid userId, Guid itemId);
    Task TransferFromSellerToBuyerAsync(Guid sellerId, Guid buyerId, Guid itemId);
}
