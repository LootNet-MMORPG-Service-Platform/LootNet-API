namespace LootNet_API.Models;

using Enums;
using LootNet_API.Models.Items;
using LootNet_API.Models.Items.Generation;

public class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string? EmailVerificationTokenHash { get; set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; set; }
    public string? PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }
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
    public string? ProfileImagePath { get; set; }
}
