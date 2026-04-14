using LootNet_API.Enums;

namespace LootNet_API.DTO.Admin;

public class AdminUserListDTO
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public UserRole Role { get; set; }
    public decimal Currency { get; set; }
    public bool IsBlocked { get; set; }
}
