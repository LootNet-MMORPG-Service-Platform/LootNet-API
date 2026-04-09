namespace LootNet_API.Data;

using LootNet_API.Models.Items;
using LootNet_API.Models.Market;
using Microsoft.EntityFrameworkCore;
using Models;

public class AppDbContext : DbContext
{
    public DbSet<ItemGenerationRule> ItemGenerationRules { get; set; }
    public DbSet<GenerationProfile> GenerationProfiles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Weapon> Weapons { get; set; }
    public DbSet<Armor> Armors { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<MarketListing> MarketListings { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
