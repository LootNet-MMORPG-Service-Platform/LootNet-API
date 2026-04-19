using LootNet_API.DTO.Items;
using LootNet_API.Models.Items;

namespace LootNet_API.Services.Interfaces;

public interface IInventoryService
{
    Task<ItemCollectionDTO> GetItemsAsync(Guid userId);
    Task<ItemCollectionDTO> GetInventoryAsync(Guid userId);
    Task<ItemCollectionDTO> GetRunInventoryAsync(Guid userId);
    Task<ItemCollectionDTO> GetMarketInventoryAsync(Guid userId);
    Task<EquipmentResponseDTO> GetEquipmentAsync(Guid userId);

    Task EquipWeaponAsync(Guid userId, Guid itemId, int slot);
    Task EquipWeaponFromRunAsync(Guid userId, Guid itemId, int slot);
    Task EquipArmorAsync(Guid userId, Guid itemId);
    Task EquipArmorFromRunAsync(Guid userId, Guid itemId);
    Task UnequipItemAsync(Guid userId, Guid itemId);

    Task MoveToRunAsync(Guid userId, List<Guid> itemIds);
    Task ReturnFromRunAsync(Guid userId);
    Task MoveToMarketAsync(Guid userId, Guid itemId);
    Task ReturnFromMarketAsync(Guid userId, Guid itemId);

    Task AddToInventoryAsync(Guid userId, Guid itemId);
    Task AddToRunInventoryAsync(Guid userId, Guid itemId);
    Task TransferFromSellerToBuyerAsync(Guid sellerId, Guid buyerId, Guid itemId);
}
