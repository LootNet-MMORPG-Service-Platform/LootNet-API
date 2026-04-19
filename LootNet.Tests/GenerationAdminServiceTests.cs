namespace LootNet_API.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LootNet_API.Data;
using LootNet_API.DTO.Generation.Create;
using LootNet_API.DTO.Generation.Update;
using LootNet_API.Enums;
using LootNet_API.Models.Items.Generation;
using LootNet_API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class GenerationAdminServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private GenerationAdminService CreateService(AppDbContext db)
        => new GenerationAdminService(db);

    [Fact]
    public async Task CreateProfile_ShouldCreateProfile()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var id = await service.CreateProfileAsync(new CreateGenerationProfileDTO
        {
            Name = "Test"
        });

        var profile = await db.GenerationProfiles.FirstAsync(x => x.Id == id);

        Assert.Equal("Test", profile.Name);
        Assert.Single(db.AdminLogs);
    }

    [Fact]
    public async Task CreateProfileFull_ShouldCreateHierarchy()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var dto = new CreateGenerationProfileFullDTO
        {
            Name = "Full",
            TypeWeights = new List<CreateTypeWeightDTO>
            {
                new CreateTypeWeightDTO
                {
                    Category = ItemCategory.Weapon,
                    WeaponType = WeaponType.Sword,
                    Weight = 1
                }
            },
            Rules = new List<CreateRuleFullDTO>
            {
                new CreateRuleFullDTO
                {
                    ProfileId = Guid.NewGuid(),
                    Category = ItemCategory.Weapon,
                    WeaponType = WeaponType.Sword,
                    IsFallback = true,
                    Parameters = new List<CreateParameterDTO>(),
                    Elements = new List<CreateElementDTO>()
                }
            }
        };

        var id = await service.CreateProfileAsync(dto);

        var profile = await db.GenerationProfiles.FirstAsync(x => x.Id == id);
        Assert.NotNull(profile);
        Assert.NotEmpty(db.ItemTypeWeights);
        Assert.NotEmpty(db.ItemGenerationRules);
        Assert.NotEmpty(db.AdminLogs);
    }

    [Fact]
    public async Task GetProfiles_ShouldReturnList()
    {
        using var db = CreateDb();
        db.GenerationProfiles.Add(new GenerationProfile { Id = Guid.NewGuid(), Name = "A" });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.GetProfilesAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task UpdateProfile_ShouldChangeName()
    {
        using var db = CreateDb();
        var profile = new GenerationProfile { Id = Guid.NewGuid(), Name = "Old" };
        db.GenerationProfiles.Add(profile);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await service.UpdateProfileAsync(new UpdateGenerationProfileDTO
        {
            Id = profile.Id,
            Name = "New"
        });

        var updated = await db.GenerationProfiles.FirstAsync(x => x.Id == profile.Id);

        Assert.Equal("New", updated.Name);
    }

    [Fact]
    public async Task DeleteProfile_ShouldRemove()
    {
        using var db = CreateDb();
        var profile = new GenerationProfile { Id = Guid.NewGuid(), Name = "X" };
        db.GenerationProfiles.Add(profile);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await service.DeleteProfileAsync(profile.Id);

        Assert.Empty(db.GenerationProfiles);
    }

    [Fact]
    public async Task CreateRule_ShouldFail_WhenWeaponAndArmorSet()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateRuleAsync(Guid.NewGuid(), new CreateRuleDTO
            {
                Category = ItemCategory.Weapon,
                WeaponType = WeaponType.Sword,
                ArmorType = ArmorType.Head,
                IsFallback = false
            }));
    }

    [Fact]
    public async Task UpdateWeight_ShouldPersistChanges()
    {
        using var db = CreateDb();

        var profile = new GenerationProfile { Id = Guid.NewGuid(), Name = "P" };
        db.GenerationProfiles.Add(profile);

        var weight = new ItemTypeWeight
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Weight = 1
        };

        db.ItemTypeWeights.Add(weight);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await service.UpdateWeightAsync(new UpdateTypeWeightDTO
        {
            Id = weight.Id,
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Weight = 5
        });

        var updated = await db.ItemTypeWeights.FirstAsync(x => x.Id == weight.Id);

        Assert.Equal(5, updated.Weight);
        Assert.Equal(WeaponType.Sword, updated.WeaponType);
    }

    [Fact]
    public async Task GetProfileDetails_ShouldReturnNestedData()
    {
        using var db = CreateDb();

        var profile = new GenerationProfile { Id = Guid.NewGuid(), Name = "P" };
        db.GenerationProfiles.Add(profile);

        db.ItemGenerationRules.Add(new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            Category = ItemCategory.Weapon
        });

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.GetProfileDetailsAsync(profile.Id);

        Assert.Equal(profile.Id, result.Id);
    }

    [Fact]
    public async Task DeleteRule_ShouldRemoveRule()
    {
        using var db = CreateDb();

        var rule = new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            ProfileId = Guid.NewGuid(),
            Category = ItemCategory.Weapon
        };

        db.ItemGenerationRules.Add(rule);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await service.DeleteRuleAsync(rule.Id);

        Assert.Empty(db.ItemGenerationRules);
    }

    [Fact]
    public async Task CreateWeight_ShouldValidateAndStore()
    {
        using var db = CreateDb();
        var profile = new GenerationProfile { Id = Guid.NewGuid(), Name = "P" };
        db.GenerationProfiles.Add(profile);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var id = await service.CreateWeightAsync(profile.Id, new CreateTypeWeightDTO
        {
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Weight = 10
        });

        var w = await db.ItemTypeWeights.FirstAsync(x => x.Id == id);

        Assert.Equal(10, w.Weight);
    }

    [Fact]
    public async Task FullFlow_CreateProfileRuleWeight_ShouldWork()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var profileId = await service.CreateProfileAsync(new CreateGenerationProfileDTO
        {
            Name = "Flow"
        });

        var ruleId = await service.CreateRuleAsync(profileId, new CreateRuleDTO
        {
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            IsFallback = true
        });

        var weightId = await service.CreateWeightAsync(profileId, new CreateTypeWeightDTO
        {
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Weight = 2
        });

        Assert.NotEqual(Guid.Empty, profileId);
        Assert.NotEqual(Guid.Empty, ruleId);
        Assert.NotEqual(Guid.Empty, weightId);
    }

    [Fact]
    public async Task UpdateProfile_NonExisting_ShouldThrow()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateProfileAsync(new UpdateGenerationProfileDTO
            {
                Id = Guid.NewGuid(),
                Name = "X"
            }));
    }

    [Fact]
    public async Task DeleteProfile_NonExisting_ShouldThrow()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteProfileAsync(Guid.NewGuid()));
    }
}