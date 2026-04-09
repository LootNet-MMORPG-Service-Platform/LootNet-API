namespace LootNet_API.Models;
using Enums;
using LootNet_API.Models.Items;

public class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; }

    public Guid ProfileId { get; set; }
    public GenerationProfile? Profile { get; set; }
    public DateTime? LastDailyReward { get; set; }
    public decimal Currency { get; set; }
}
