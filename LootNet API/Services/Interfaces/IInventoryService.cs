using LootNet_API.DTO.Items;
using LootNet_API.Models.Items;

namespace LootNet_API.Services.Interfaces;

public interface IInventoryService
{
    Task<ItemCollectionDTO> GetItemsAsync(Guid userId);
    Task<ItemCollectionDTO> GetInventoryAsync(Guid userId);

    Task<EquipmentResponseDTO> GetEquipmentAsync(Guid userId);

    Task EquipWeaponAsync(Guid userId, Guid itemId, int slot);

    Task EquipArmorAsync(Guid userId, Guid itemId);

    Task UnequipItemAsync(Guid userId, Guid itemId);
}
