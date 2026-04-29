namespace LootNet_API.Models;

using Enums;
using LootNet_API.Models.Items;
using LootNet_API.Models.Items.Generation;

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
    public required Equipment Equipment { get; set; }
    public List<InventoryItem> InventoryItems { get; set; } = new();
    public List<MarketInventoryItem> MarketInventoryItems { get; set; } = new();
    public List<RunInventoryItem> RunInventoryItems { get; set; } = new();
    public bool IsBlocked { get; set; }
    public DateTime? BlockedUntil { get; set; }
    public string? BlockReason { get; set; }
}
