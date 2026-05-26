namespace LootNet_API.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LootNet_API.Controllers;
using LootNet_API.Data;
using LootNet_API.DTO.Generation.Create;
using LootNet_API.DTO.Generation.Update;
using LootNet_API.Enums;
using LootNet_API.Models.Items.Generation;
using LootNet_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
                ArmorType = ArmorType.Helmet,
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
    public async Task UpdateParameter_ShouldReplaceSegments()
    {
        using var db = CreateDb();
        var parameter = new ItemParameterSetting
        {
            Id = Guid.NewGuid(),
            RuleId = Guid.NewGuid(),
            Parameter = ItemParameter.CutDamage,
            Segments = new List<DistributionSegment>
            {
                new() { Id = Guid.NewGuid(), Min = 1, Max = 2, Weight = 3 }
            }
        };
        db.ItemParameterSettings.Add(parameter);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await service.UpdateParameterAsync(new UpdateParameterDTO
        {
            Id = parameter.Id,
            Parameter = ItemParameter.BluntDamage,
            Segments = new List<CreateSegmentDTO>
            {
                new() { Min = 10, Max = 20, Weight = 30 },
                new() { Min = 40, Max = 50, Weight = 60 }
            }
        });

        var updated = await db.ItemParameterSettings
            .Include(x => x.Segments)
            .FirstAsync(x => x.Id == parameter.Id);

        Assert.Equal(ItemParameter.BluntDamage, updated.Parameter);
        Assert.Equal(2, updated.Segments.Count);
        Assert.Contains(updated.Segments, x => x.Min == 10 && x.Max == 20 && x.Weight == 30);
        Assert.Contains(updated.Segments, x => x.Min == 40 && x.Max == 50 && x.Weight == 60);
    }

    [Fact]
    public async Task UpdateElement_ShouldReplaceSegments()
    {
        using var db = CreateDb();
        var element = new ItemElementSetting
        {
            Id = Guid.NewGuid(),
            RuleId = Guid.NewGuid(),
            ElementType = ItemElementType.Fire,
            Segments = new List<DistributionSegment>
            {
                new() { Id = Guid.NewGuid(), Min = 1, Max = 2, Weight = 3 }
            }
        };
        db.ItemElementSettings.Add(element);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        await service.UpdateElementAsync(new UpdateElementDTO
        {
            Id = element.Id,
            ElementType = ItemElementType.Water,
            Segments = new List<CreateSegmentDTO>
            {
                new() { Min = 7, Max = 8, Weight = 9 }
            }
        });

        var updated = await db.ItemElementSettings
            .Include(x => x.Segments)
            .FirstAsync(x => x.Id == element.Id);

        var segment = Assert.Single(updated.Segments);
        Assert.Equal(ItemElementType.Water, updated.ElementType);
        Assert.Equal(7, segment.Min);
        Assert.Equal(8, segment.Max);
        Assert.Equal(9, segment.Weight);
    }

    [Fact]
    public async Task GenerationController_UpdateParameter_LogsCurrentAdminId()
    {
        using var db = CreateDb();
        var adminId = Guid.NewGuid();
        var parameter = new ItemParameterSetting
        {
            Id = Guid.NewGuid(),
            RuleId = Guid.NewGuid(),
            Parameter = ItemParameter.CutDamage,
            Segments = new List<DistributionSegment>
            {
                new() { Id = Guid.NewGuid(), Min = 1, Max = 2, Weight = 3 }
            }
        };
        db.ItemParameterSettings.Add(parameter);
        await db.SaveChangesAsync();

        var controller = CreateController(db, adminId);

        var result = await controller.UpdateParameter(new UpdateParameterDTO
        {
            Id = parameter.Id,
            Parameter = ItemParameter.BluntDamage,
            Segments = new List<CreateSegmentDTO>
            {
                new() { Min = 4, Max = 5, Weight = 6 }
            }
        });

        Assert.IsType<OkResult>(result);
        var log = await db.AdminLogs.SingleAsync();
        Assert.Equal(adminId, log.AdminId);
        Assert.Equal("UPDATE_PARAMETER", log.Action);
    }

    [Fact]
    public async Task GenerationController_UpdateElement_LogsCurrentAdminId()
    {
        using var db = CreateDb();
        var adminId = Guid.NewGuid();
        var element = new ItemElementSetting
        {
            Id = Guid.NewGuid(),
            RuleId = Guid.NewGuid(),
            ElementType = ItemElementType.Fire,
            Segments = new List<DistributionSegment>
            {
                new() { Id = Guid.NewGuid(), Min = 1, Max = 2, Weight = 3 }
            }
        };
        db.ItemElementSettings.Add(element);
        await db.SaveChangesAsync();

        var controller = CreateController(db, adminId);

        var result = await controller.UpdateElement(new UpdateElementDTO
        {
            Id = element.Id,
            ElementType = ItemElementType.Water,
            Segments = new List<CreateSegmentDTO>
            {
                new() { Min = 4, Max = 5, Weight = 6 }
            }
        });

        Assert.IsType<OkResult>(result);
        var log = await db.AdminLogs.SingleAsync();
        Assert.Equal(adminId, log.AdminId);
        Assert.Equal("UPDATE_ELEMENT", log.Action);
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

    private static GenerationAdminController CreateController(AppDbContext db, Guid adminId)
    {
        return new GenerationAdminController(new GenerationAdminService(db))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, adminId.ToString())
                    }))
                }
            }
        };
    }
}
