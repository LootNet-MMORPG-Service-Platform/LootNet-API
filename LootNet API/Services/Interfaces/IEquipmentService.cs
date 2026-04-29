using LootNet_API.DTO.Items;
using LootNet_API.Models.Items;

namespace LootNet_API.Services.Interfaces;

public interface IEquipmentService
{
    Task<EquipmentResponseDTO> GetEquipmentAsync(Guid userId);

    Task EquipWeaponAsync(Guid userId, Guid itemId, int slot);
    Task EquipWeaponFromRunAsync(Guid userId, Guid itemId, int slot);

    Task EquipArmorAsync(Guid userId, Guid itemId);
    Task EquipArmorFromRunAsync(Guid userId, Guid itemId);

    Task UnequipItemAsync(Guid userId, Guid itemId);

    void ApplyEnemyEquipment(Equipment equipment, List<Item> items);
    Task<WeaponDTO> GetWeapon(Guid? id);
    Task<ArmorDTO> GetArmor(Guid? id);
}
