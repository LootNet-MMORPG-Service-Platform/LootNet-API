namespace LootNet_API.Tests;

using LootNet_API.Data;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Services;
using Microsoft.EntityFrameworkCore;
using LootNet_API.Tests.Helpers;
using Xunit;
using LootNet_API.Services.Interfaces;
using LootNet_API.Models.Items.Generation;

public class ItemGeneratorTests
{


    private IItemNameGenerator GetNameGenerator() => new ItemNameGenerator();

    [Fact]
    public async Task GeneratesWeaponWithParametersAndElements()
    {
        var db = TestDbContextFactory.Create();

        var profile = new GenerationProfile
        {
            Id = Guid.NewGuid(),
            Name = "TestProfile",
            TypeWeights = new List<ItemTypeWeight>
            {
                new() {  Category = ItemCategory.Weapon , Weight = 1 }
            }
        };

        var rule = new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Parameters = new List<ItemParameterSetting>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Parameter = ItemParameter.CutDamage,
                    Segments = new List<DistributionSegment>
                    {
                        new() { Min = 10, Max = 20, Weight = 1 }
                    }
                }
            },
            Elements = new List<ItemElementSetting>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ElementType = ItemElementType.Fire,
                    Segments = new List<DistributionSegment>
                    {
                        new() { Min = 5, Max = 10, Weight = 1 }
                    }
                }
            }
        };

        profile.Rules.Add(rule);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "player1",
            PasswordHash = "hash",
            Role = UserRole.Player,
            Equipment = new Equipment(),
            ProfileId = profile.Id,
            Profile = profile
        };

        db.Users.Add(user);
        db.GenerationProfiles.Add(profile);
        db.ItemGenerationRules.Add(rule);
        await db.SaveChangesAsync();

        var generator = new ItemGenerationService(db, GetNameGenerator());

        var item = await generator.GenerateItemAsync(user.Id) as Weapon;

        Assert.NotNull(item);
        Assert.Equal(WeaponType.Sword, item.WeaponType);
        Assert.InRange(item.Cut, 10, 20);
        Assert.Single(item.Elements);
        Assert.Equal(ItemElementType.Fire, item.Elements.First().ItemElementType);
        Assert.InRange(item.Elements.First().Value, 5, 10);
        Assert.False(string.IsNullOrEmpty(item.Name));
    }

    [Fact]
    public async Task UsesFallbackWhenProfileHasNoRule()
    {
        var db = TestDbContextFactory.Create();

        var profile = new GenerationProfile
        {
            Id = Guid.NewGuid(),
            Name = "EmptyProfile",
            TypeWeights = new List<ItemTypeWeight>
            {
                new() { Category = ItemCategory.Weapon, Weight = 1 }
            }
        };

        var fallback = new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Axe,
            IsFallback = true,
            Parameters = new List<ItemParameterSetting>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Parameter = ItemParameter.CutDamage,
                    Segments = new List<DistributionSegment> { new() { Min = 1, Max = 2, Weight = 1 } }
                }
            }
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "player2",
            PasswordHash = "hash",
            Equipment = new Equipment(),
            Role = UserRole.Player,
            ProfileId = profile.Id,
            Profile = profile
        };

        db.Users.Add(user);
        db.GenerationProfiles.Add(profile);
        db.ItemGenerationRules.Add(fallback);
        await db.SaveChangesAsync();

        var generator = new ItemGenerationService(db, GetNameGenerator());

        var item = await generator.GenerateItemAsync(user.Id) as Weapon;

        Assert.NotNull(item);
        Assert.Equal(WeaponType.Axe, item.WeaponType);
        Assert.InRange(item.Cut, 1, 2);
    }

    [Fact]
    public async Task ThrowsWhenNoTypeWeights()
    {
        var db = TestDbContextFactory.Create();
        var profile = new GenerationProfile { Id = Guid.NewGuid(), Name = "EmptyWeights" };
        var user = new User { Id = Guid.NewGuid(), Username = "p", PasswordHash = "h", 
            Role = UserRole.Player, ProfileId = profile.Id, Profile = profile, Equipment = new Equipment()
        };
        db.Users.Add(user);
        db.GenerationProfiles.Add(profile);
        await db.SaveChangesAsync();

        var generator = new ItemGenerationService(db, GetNameGenerator());

        await Assert.ThrowsAsync<InvalidOperationException>(() => generator.GenerateItemAsync(user.Id));
    }

    [Fact]
    public async Task ThrowsWhenNoFallback()
    {
        var db = TestDbContextFactory.Create();

        var profile = new GenerationProfile
        {
            Id = Guid.NewGuid(),
            Name = "EmptyProfile",
            TypeWeights = new List<ItemTypeWeight>
            {
                new() { Category = ItemCategory.Weapon, Weight = 1 }
            }
        };

        var user = new User { Id = Guid.NewGuid(), Username = "p", PasswordHash = "h",
            Role = UserRole.Player, ProfileId = profile.Id, Profile = profile, Equipment = new Equipment()
        };

        db.Users.Add(user);
        db.GenerationProfiles.Add(profile);
        await db.SaveChangesAsync();

        var generator = new ItemGenerationService(db, GetNameGenerator());

        await Assert.ThrowsAsync<InvalidOperationException>(() => generator.GenerateItemAsync(user.Id));
    }

    [Fact]
    public async Task GeneratesArmorCorrectly()
    {
        var db = TestDbContextFactory.Create();

        var profile = new GenerationProfile
        {
            Id = Guid.NewGuid(),
            Name = "ArmorProfile",
            TypeWeights = new List<ItemTypeWeight>
            {
                new() { Category = ItemCategory.Armor, Weight = 1 }
            }
        };

        var rule = new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            Category = ItemCategory.Armor,
            ArmorType = ArmorType.Body,
            Parameters = new List<ItemParameterSetting>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Parameter = ItemParameter.BluntResistance,
                    Segments = new List<DistributionSegment> { new() { Min = 5, Max = 10, Weight = 1 } }
                }
            },
            Elements = new List<ItemElementSetting>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ElementType = ItemElementType.Ice,
                    Segments = new List<DistributionSegment> { new() { Min = 2, Max = 4, Weight = 1 } }
                }
            }
        };

        profile.Rules.Add(rule);
        var user = new User { Id = Guid.NewGuid(), Username = "p3", PasswordHash = "h",
            Role = UserRole.Player, ProfileId = profile.Id, Profile = profile, Equipment = new Equipment()
        };

        db.Users.Add(user);
        db.GenerationProfiles.Add(profile);
        db.ItemGenerationRules.Add(rule);
        await db.SaveChangesAsync();

        var generator = new ItemGenerationService(db, GetNameGenerator());

        var item = await generator.GenerateItemAsync(user.Id) as Armor;

        Assert.NotNull(item);
        Assert.Equal(ArmorType.Body, item.ArmorType);
        Assert.InRange(item.BluntResistance, 5, 10);
        Assert.Single(item.Elements);
        Assert.InRange(item.Elements.First().Value, 2, 4);
    }

    [Fact]
    public async Task GeneratesMultipleItemsConsistently()
    {
        var db = TestDbContextFactory.Create();

        var profile = new GenerationProfile
        {
            Id = Guid.NewGuid(),
            Name = "MultiProfile",
            TypeWeights = new List<ItemTypeWeight>
            {
                new() { Category = ItemCategory.Weapon, Weight = 50 },
                new() { Category = ItemCategory.Armor, Weight = 50 }
            }
        };

        var weaponRule = new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Parameters = new List<ItemParameterSetting>
            {
                new() { Id = Guid.NewGuid(), Parameter = ItemParameter.BluntDamage, Segments = new List<DistributionSegment> { new() { Min = 15, Max = 25, Weight = 1 } } }
            }
        };

        var armorRule = new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            Category = ItemCategory.Armor,
            ArmorType = ArmorType.Boots,
            Parameters = new List<ItemParameterSetting>
            {
                new() { Id = Guid.NewGuid(), Parameter = ItemParameter.CutResistance, Segments = new List<DistributionSegment> { new() { Min = 3, Max = 6, Weight = 1 } } }
            }
        };

        profile.Rules.Add(weaponRule);
        profile.Rules.Add(armorRule);

        var user = new User { Id = Guid.NewGuid(), Username = "p4", PasswordHash = "h", 
            Role = UserRole.Player, ProfileId = profile.Id, Profile = profile, Equipment = new Equipment()
        };

        db.Users.Add(user);
        db.GenerationProfiles.Add(profile);
        db.ItemGenerationRules.AddRange(weaponRule, armorRule);
        await db.SaveChangesAsync();

        var generator = new ItemGenerationService(db, GetNameGenerator());

        var generated = new List<Item>();
        for (int i = 0; i < 50; i++)
        {
            var item = await generator.GenerateItemAsync(user.Id);
            generated.Add(item);
        }

        Assert.All(generated.Where(i => i is Weapon), w => Assert.InRange(((Weapon)w).Blunt, 15, 25));
        Assert.All(generated.Where(i => i is Armor), a => Assert.InRange(((Armor)a).CutResistance, 3, 6));
    }
}