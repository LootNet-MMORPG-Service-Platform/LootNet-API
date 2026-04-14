namespace LootNet_API.Data;

using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Models.Logs;
using LootNet_API.Models.Market;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Weapon> Weapons { get; set; }
    public DbSet<Armor> Armors { get; set; }
    public DbSet<ItemElement> ItemElements { get; set; }
    public DbSet<Equipment> Equipments { get; set; }
    public DbSet<MarketListing> MarketListings { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<GenerationProfile> GenerationProfiles { get; set; }
    public DbSet<ItemGenerationRule> ItemGenerationRules { get; set; }
    public DbSet<ItemTypeWeight> ItemTypeWeights { get; set; }
    public DbSet<ItemParameterSetting> ItemParameterSettings { get; set; }
    public DbSet<ItemElementSetting> ItemElementSettings { get; set; }
    public DbSet<DistributionSegment> DistributionSegments { get; set; }
    public DbSet<AdminLog> AdminLogs { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Equipment)
            .WithOne()
            .HasForeignKey<Equipment>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GenerationProfile>()
            .HasMany(x => x.Rules)
            .WithOne()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GenerationProfile>()
            .HasMany(x => x.TypeWeights)
            .WithOne()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ItemGenerationRule>()
            .HasMany(x => x.Parameters)
            .WithOne()
            .HasForeignKey(x => x.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ItemGenerationRule>()
            .HasMany(x => x.Elements)
            .WithOne()
            .HasForeignKey(x => x.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ItemParameterSetting>()
            .HasMany(x => x.Segments)
            .WithOne()
            .HasForeignKey(x => x.ItemParameterSettingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ItemElementSetting>()
            .HasMany(x => x.Segments)
            .WithOne()
            .HasForeignKey(x => x.ItemElementSettingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Weapon>()
            .HasMany(x => x.Elements)
            .WithOne()
            .HasForeignKey(x => x.WeaponId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Armor>()
            .HasMany(x => x.Elements)
            .WithOne()
            .HasForeignKey(x => x.ArmorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ItemElement>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_ItemElement_OnlyOneOwner",
                @"(WeaponId IS NULL AND ArmorId IS NOT NULL)
                  OR
                  (WeaponId IS NOT NULL AND ArmorId IS NULL)"
            ));

        modelBuilder.Entity<DistributionSegment>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_DistributionSegment_OnlyOneParent",
                @"(ItemParameterSettingId IS NULL AND ItemElementSettingId IS NOT NULL)
                  OR
                  (ItemParameterSettingId IS NOT NULL AND ItemElementSettingId IS NULL)"
            ));

        modelBuilder.Entity<MarketListing>()
            .HasIndex(x => x.Price);

        modelBuilder.Entity<MarketListing>()
            .HasIndex(x => x.Category);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<ItemGenerationRule>()
            .HasIndex(x => x.ProfileId);

        modelBuilder.Entity<ItemTypeWeight>()
            .HasIndex(x => x.ProfileId);

        modelBuilder.Entity<ItemParameterSetting>()
            .HasIndex(x => x.RuleId);

        modelBuilder.Entity<ItemElementSetting>()
            .HasIndex(x => x.RuleId);
    }
}