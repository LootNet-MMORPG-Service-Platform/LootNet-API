using LootNet_API.DTO;
using LootNet_API.DTO.Admin;
using LootNet_API.DTO.Items;
using LootNet_API.Enums;

namespace LootNet_API.Services.Interfaces;

public interface IAdminService
{
    Task<PagedResultDTO<AdminUserListDTO>> GetUsersAsync(GetUsersQueryDTO query);

    Task<AdminUserDetailsDTO> GetUserAsync(Guid id);

    Task BlockUserAsync(Guid adminId, Guid userId, string reason, int? days);

    Task UnblockUserAsync(Guid adminId, Guid userId);

    Task ChangeRoleAsync(Guid adminId, Guid userId, UserRole role);
    Task<ItemCollectionDTO> GetUserInventoryAsync(Guid userId);
    Task<EquipmentResponseDTO> GetUserEquipmentAsync(Guid userId);
}
