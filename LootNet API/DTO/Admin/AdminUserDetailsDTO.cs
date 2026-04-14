using LootNet_API.DTO.Items;
using LootNet_API.Enums;

namespace LootNet_API.DTO.Admin;

public class AdminUserDetailsDTO
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public UserRole Role { get; set; }
    public decimal Currency { get; set; }

    public bool IsBlocked { get; set; }
    public DateTime? BlockedUntil { get; set; }
    public string? BlockReason { get; set; }
}
