using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class UserProfileDTO
{
    public Guid Id { get; set; }
    public string Username { get; set; } = default!;
    public UserRole Role { get; set; }
    public decimal Currency { get; set; }
}
