using LootNet_API.Data;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Services.Interfaces;

public static class DbSeeder
{
    public static void Seed(AppDbContext context, IItemGenerationService generator)
    {
        var profile = new GenerationProfile
        {
            Id = Guid.NewGuid(),
            Name = "Default",
            TypeWeights = new List<ItemTypeWeight>
            {
             new() { Category = ItemCategory.Weapon, Weight = 100 },
            }
        };

        var weaponRule = new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword
        };

        var cutParam = new ItemParameterSetting
        {
            Id = Guid.NewGuid(),
            RuleId = weaponRule.Id,
            Parameter = ItemParameter.CutDamage,
            Segments = new List<DistributionSegment>
            {
                new() { Min = 10, Max = 20, Weight = 70 },
                new() { Min = 20, Max = 40, Weight = 30 }
            }
        };
        weaponRule.Parameters.Add(cutParam);

        var fireElement = new ItemElementSetting
        {
            Id = Guid.NewGuid(),
            RuleId = weaponRule.Id,
            ElementType = ItemElementType.Fire,
            Segments = new List<DistributionSegment>
            {
                new() { Min = 5, Max = 10, Weight = 100 }
            }
        };
        weaponRule.Elements.Add(fireElement);

        var fallbackWeapon = new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            Category = ItemCategory.Weapon,
            ProfileId = profile.Id,
            WeaponType = WeaponType.Axe,
            IsFallback = true,
            Parameters = new List<ItemParameterSetting>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Parameter = ItemParameter.CutDamage,
                    Segments = new List<DistributionSegment>
                    {
                        new() { Min = 1, Max = 5, Weight = 100 }
                    }
                }
            }
        };

        profile.Rules.Add(weaponRule);

        context.GenerationProfiles.Add(profile);
        context.ItemGenerationRules.AddRange(weaponRule, fallbackWeapon);
        context.SaveChanges();

        var users = new List<User>();
        for (int i = 1; i <= 5; i++)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = $"Player{i}",
                PasswordHash = "hashedpassword",
                Role = UserRole.Player,
                ProfileId = profile.Id,
                Currency = 1000,
                Equipment = new Equipment()
            };
            users.Add(user);
        }

        context.Users.AddRange(users);
        context.SaveChanges();

        foreach (var user in users)
        {
            for (int j = 0; j < 3; j++)
            {
                var item = generator.GenerateItemAsync(user.Id).Result;

                if (item is Weapon w)
                {
                    Console.WriteLine($"Generated Weapon ID: {w.Id} for user {user.Username}");
                    if (context.Weapons.Any(w => w.Id == item.Id))
                    {
                        Console.WriteLine("Duplicate detected! Regenerating...");
                        item.Id = Guid.NewGuid();
                    }
                    context.Weapons.Add(w);
                }
                else if (item is Armor a)
                    context.Armors.Add(a);
            }
        }

        context.SaveChanges();
    }
}