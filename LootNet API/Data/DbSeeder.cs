using LootNet_API.Data;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.GameRun;
using LootNet_API.Models.GameRun.EnemyGeneration;
using LootNet_API.Models.Items;
using LootNet_API.Models.Items.Generation;
using LootNet_API.Models.Logs;
using LootNet_API.Models.Market;
using LootNet_API.Services.Interfaces;

public static class DbSeeder
{
    public static void Seed(AppDbContext context, IItemGenerationService generator)
    {
        var now = DateTime.UtcNow;

        var defaultProfile = BuildDefaultGenerationProfile();
        var weakEnemyProfile = BuildWeakEnemyGenerationProfile();
        var eliteProfile = BuildEliteGenerationProfile();

        context.GenerationProfiles.AddRange(defaultProfile, weakEnemyProfile, eliteProfile);
        context.SaveChanges();

        var classProfiles = BuildEnemyClassProfiles(weakEnemyProfile.Id, eliteProfile.Id);
        context.EnemyClassProfiles.AddRange(classProfiles);

        var stages = BuildStageProfiles(classProfiles);
        context.StageProfiles.AddRange(stages);
        context.SaveChanges();

        var users = BuildUsers(defaultProfile.Id);
        context.Users.AddRange(users);
        context.SaveChanges();

        foreach (var user in users)
        {
            context.RefreshTokens.AddRange(
                new RefreshToken { UserId = user.Id, Token = $"seed-refresh-{user.Username.ToLower()}-a", ExpiresAt = now.AddDays(7) },
                new RefreshToken { UserId = user.Id, Token = $"seed-refresh-{user.Username.ToLower()}-b", ExpiresAt = now.AddDays(14) },
                new RefreshToken { UserId = user.Id, Token = $"seed-refresh-{user.Username.ToLower()}-c", ExpiresAt = now.AddDays(21) });
        }

        context.SaveChanges();

        SeedItemsAndInventories(context, generator, users);

        SeedMarketplace(context, users, now);
        SeedChat(context, users, now);
        SeedRuns(context, users, now);
        SeedAdminArtifacts(context, users, now);

        context.SaveChanges();
    }

    private static GenerationProfile BuildDefaultGenerationProfile()
    {
        var profile = new GenerationProfile
        {
            Id = Guid.NewGuid(),
            Name = "Default"
        };

        profile.TypeWeights.AddRange(
        [
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.TwoHandSword, Weight = 18 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Sword, Weight = 45 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Shortsword, Weight = 25 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Bow, Weight = 15 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Crossbow, Weight = 10 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Polearm, Weight = 12 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Chestplate, Weight = 30 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Helmet, Weight = 20 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Greaves, Weight = 20 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Sabatons, Weight = 15 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Gauntlets, Weight = 15 }
        ]);

        profile.Rules.AddRange(
        [
            CreateWeaponRule(profile.Id, WeaponType.Sword, false, 12, 24, 5, 14),
            CreateWeaponRule(profile.Id, WeaponType.Shortsword, false, 10, 22, 3, 12),
            CreateWeaponRule(profile.Id, WeaponType.Bow, false, 11, 21, 2, 9),
            CreateWeaponRule(profile.Id, WeaponType.Crossbow, false, 13, 24, 3, 10),
            CreateWeaponRule(profile.Id, WeaponType.Polearm, false, 14, 26, 4, 13),
            CreateWeaponRule(profile.Id, WeaponType.TwoHandSword, false, 16, 30, 6, 16),
            CreateWeaponRule(profile.Id, WeaponType.Sword, true, 3, 8, 1, 5),

            CreateArmorRule(profile.Id, ArmorType.Helmet, false, 5, 14, 2, 8),
            CreateArmorRule(profile.Id, ArmorType.Chestplate, false, 8, 18, 4, 10),
            CreateArmorRule(profile.Id, ArmorType.Greaves, false, 6, 15, 2, 8),
            CreateArmorRule(profile.Id, ArmorType.Sabatons, false, 4, 10, 2, 7),
            CreateArmorRule(profile.Id, ArmorType.Gauntlets, false, 4, 10, 2, 7),
            CreateArmorRule(profile.Id, ArmorType.Chestplate, true, 2, 6, 1, 4)
        ]);

        return profile;
    }

    private static GenerationProfile BuildEliteGenerationProfile()
    {
        var profile = new GenerationProfile
        {
            Id = Guid.NewGuid(),
            Name = "Elite"
        };

        profile.TypeWeights.AddRange(
        [
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.TwoHandSword, Weight = 40 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Polearm, Weight = 35 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Crossbow, Weight = 15 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Helmet, Weight = 16 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Chestplate, Weight = 25 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Gauntlets, Weight = 14 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Greaves, Weight = 15 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Sabatons, Weight = 12 }
        ]);

        profile.Rules.AddRange(
        [
            CreateWeaponRule(profile.Id, WeaponType.TwoHandSword, false, 18, 38, 8, 22),
            CreateWeaponRule(profile.Id, WeaponType.Polearm, false, 16, 34, 7, 20),
            CreateWeaponRule(profile.Id, WeaponType.Crossbow, false, 14, 30, 5, 14),
            CreateWeaponRule(profile.Id, WeaponType.TwoHandSword, true, 8, 16, 3, 10),
            CreateArmorRule(profile.Id, ArmorType.Helmet, false, 10, 20, 4, 12),
            CreateArmorRule(profile.Id, ArmorType.Chestplate, false, 14, 28, 6, 16),
            CreateArmorRule(profile.Id, ArmorType.Gauntlets, false, 9, 18, 4, 11),
            CreateArmorRule(profile.Id, ArmorType.Greaves, false, 10, 20, 4, 12),
            CreateArmorRule(profile.Id, ArmorType.Sabatons, false, 8, 16, 3, 10),
            CreateArmorRule(profile.Id, ArmorType.Chestplate, true, 7, 14, 3, 8)
        ]);

        return profile;
    }

    private static GenerationProfile BuildWeakEnemyGenerationProfile()
    {
        var profile = new GenerationProfile
        {
            Id = Guid.NewGuid(),
            Name = "WeakEnemy"
        };

        profile.TypeWeights.AddRange(
        [
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Sword, Weight = 30 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Shortsword, Weight = 30 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Bow, Weight = 20 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Crossbow, Weight = 20 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Helmet, Weight = 20 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Chestplate, Weight = 25 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Gauntlets, Weight = 15 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Greaves, Weight = 20 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Sabatons, Weight = 20 }
        ]);

        profile.Rules.AddRange(
        [
            CreateWeaponRule(profile.Id, WeaponType.Sword, false, 1, 3, 1, 3),
            CreateWeaponRule(profile.Id, WeaponType.Shortsword, false, 1, 3, 1, 3),
            CreateWeaponRule(profile.Id, WeaponType.Bow, false, 1, 3, 1, 2),
            CreateWeaponRule(profile.Id, WeaponType.Crossbow, false, 1, 3, 1, 2),
            CreateArmorRule(profile.Id, ArmorType.Helmet, false, 1, 3, 1, 3),
            CreateArmorRule(profile.Id, ArmorType.Chestplate, false, 1, 3, 1, 3),
            CreateArmorRule(profile.Id, ArmorType.Gauntlets, false, 1, 3, 1, 3),
            CreateArmorRule(profile.Id, ArmorType.Greaves, false, 1, 3, 1, 3),
            CreateArmorRule(profile.Id, ArmorType.Sabatons, false, 1, 3, 1, 3),
            CreateWeaponRule(profile.Id, WeaponType.Sword, true, 1, 2, 1, 2),
            CreateArmorRule(profile.Id, ArmorType.Chestplate, true, 1, 2, 1, 2)
        ]);

        return profile;
    }

    private static ItemGenerationRule CreateWeaponRule(Guid profileId, WeaponType type, bool isFallback, int cutMin, int cutMax, int bluntMin, int bluntMax)
    {
        var rule = new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            Category = ItemCategory.Weapon,
            WeaponType = type,
            IsFallback = isFallback
        };

        rule.Parameters.Add(new ItemParameterSetting
        {
            Id = Guid.NewGuid(),
            RuleId = rule.Id,
            Parameter = ItemParameter.CutDamage,
            Segments = new List<DistributionSegment> { new() { Min = cutMin, Max = cutMax, Weight = 100 } }
        });

        rule.Parameters.Add(new ItemParameterSetting
        {
            Id = Guid.NewGuid(),
            RuleId = rule.Id,
            Parameter = ItemParameter.BluntDamage,
            Segments = new List<DistributionSegment> { new() { Min = bluntMin, Max = bluntMax, Weight = 100 } }
        });

        rule.Elements.Add(new ItemElementSetting
        {
            Id = Guid.NewGuid(),
            RuleId = rule.Id,
            ElementType = ItemElementType.Fire,
            Segments = new List<DistributionSegment> { new() { Min = 1, Max = 8, Weight = 100 } }
        });

        return rule;
    }

    private static ItemGenerationRule CreateArmorRule(Guid profileId, ArmorType type, bool isFallback, int cutResMin, int cutResMax, int bluntResMin, int bluntResMax)
    {
        var rule = new ItemGenerationRule
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            Category = ItemCategory.Armor,
            ArmorType = type,
            IsFallback = isFallback
        };

        rule.Parameters.Add(new ItemParameterSetting
        {
            Id = Guid.NewGuid(),
            RuleId = rule.Id,
            Parameter = ItemParameter.CutResistance,
            Segments = new List<DistributionSegment> { new() { Min = cutResMin, Max = cutResMax, Weight = 100 } }
        });

        rule.Parameters.Add(new ItemParameterSetting
        {
            Id = Guid.NewGuid(),
            RuleId = rule.Id,
            Parameter = ItemParameter.BluntResistance,
            Segments = new List<DistributionSegment> { new() { Min = bluntResMin, Max = bluntResMax, Weight = 100 } }
        });

        return rule;
    }

    private static List<EnemyClassProfile> BuildEnemyClassProfiles(Guid weakProfileId, Guid eliteProfileId)
    {
        return
        [
            new EnemyClassProfile { Id = Guid.NewGuid(), Name = "Front Tank", Class = EnemyClass.Tank, AllowedColumns = [1], GenerationProfileId = eliteProfileId, Weight = 20 },
            new EnemyClassProfile { Id = Guid.NewGuid(), Name = "Halberd Guard", Class = EnemyClass.Polearm, AllowedColumns = [1,2], GenerationProfileId = eliteProfileId, Weight = 18 },
            new EnemyClassProfile { Id = Guid.NewGuid(), Name = "Skirmisher", Class = EnemyClass.Skirmisher, AllowedColumns = [1,2], GenerationProfileId = weakProfileId, Weight = 20 },
            new EnemyClassProfile { Id = Guid.NewGuid(), Name = "Crossbowman", Class = EnemyClass.Crossbow, AllowedColumns = [2,3], GenerationProfileId = weakProfileId, Weight = 16 },
            new EnemyClassProfile { Id = Guid.NewGuid(), Name = "Archer", Class = EnemyClass.Archer, AllowedColumns = [3,4], GenerationProfileId = weakProfileId, Weight = 14 },
            new EnemyClassProfile { Id = Guid.NewGuid(), Name = "Berserker", Class = EnemyClass.TwoHand, AllowedColumns = [1], GenerationProfileId = eliteProfileId, Weight = 12 }
        ];
    }

    private static List<StageProfile> BuildStageProfiles(List<EnemyClassProfile> classProfiles)
    {
        var tank = classProfiles.First(x => x.Class == EnemyClass.Tank).Id;
        var polearm = classProfiles.First(x => x.Class == EnemyClass.Polearm).Id;
        var skirmisher = classProfiles.First(x => x.Class == EnemyClass.Skirmisher).Id;
        var crossbow = classProfiles.First(x => x.Class == EnemyClass.Crossbow).Id;
        var archer = classProfiles.First(x => x.Class == EnemyClass.Archer).Id;
        var twoHand = classProfiles.First(x => x.Class == EnemyClass.TwoHand).Id;

        return
        [
            new StageProfile
            {
                Id = Guid.NewGuid(),
                Name = "Stage 1",
                StageIndex = 0,
                Weight = 100,
                Falloff = 0.25,
                Threshold = 1,
                Scenarios =
                [
                    new StageScenario
                    {
                        Id = Guid.NewGuid(),
                        EnemyCount = 2,
                        Weight = 60,
                        Slots =
                        [
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 1, ClassProfileId = skirmisher, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 3, ClassProfileId = archer, Weight = 100 }
                        ]
                    },
                    new StageScenario
                    {
                        Id = Guid.NewGuid(),
                        EnemyCount = 3,
                        Weight = 40,
                        Slots =
                        [
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 1, ClassProfileId = skirmisher, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 2, ClassProfileId = crossbow, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 4, ClassProfileId = archer, Weight = 100 }
                        ]
                    }
                ]
            },
            new StageProfile
            {
                Id = Guid.NewGuid(),
                Name = "Stage 2",
                StageIndex = 1,
                Weight = 100,
                Falloff = 0.2,
                Threshold = 1,
                Scenarios =
                [
                    new StageScenario
                    {
                        Id = Guid.NewGuid(),
                        EnemyCount = 4,
                        Weight = 100,
                        Slots =
                        [
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 1, ClassProfileId = tank, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 2, ClassProfileId = polearm, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 3, ClassProfileId = crossbow, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 4, ClassProfileId = archer, Weight = 100 }
                        ]
                    }
                ]
            },
            new StageProfile
            {
                Id = Guid.NewGuid(),
                Name = "Stage 3",
                StageIndex = 2,
                Weight = 100,
                Falloff = 0.18,
                Threshold = 1,
                Scenarios =
                [
                    new StageScenario
                    {
                        Id = Guid.NewGuid(),
                        EnemyCount = 4,
                        Weight = 70,
                        Slots =
                        [
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 1, ClassProfileId = skirmisher, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 2, ClassProfileId = skirmisher, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 3, ClassProfileId = crossbow, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 4, ClassProfileId = archer, Weight = 100 }
                        ]
                    },
                    new StageScenario
                    {
                        Id = Guid.NewGuid(),
                        EnemyCount = 3,
                        Weight = 30,
                        Slots =
                        [
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 1, ClassProfileId = tank, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 2, ClassProfileId = crossbow, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 4, ClassProfileId = archer, Weight = 100 }
                        ]
                    }
                ]
            },
            new StageProfile
            {
                Id = Guid.NewGuid(),
                Name = "Stage 4",
                StageIndex = 3,
                Weight = 100,
                Falloff = 0.15,
                Threshold = 1,
                Scenarios =
                [
                    new StageScenario
                    {
                        Id = Guid.NewGuid(),
                        EnemyCount = 4,
                        Weight = 100,
                        Slots =
                        [
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 1, ClassProfileId = tank, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 2, ClassProfileId = polearm, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 3, ClassProfileId = crossbow, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 4, ClassProfileId = archer, Weight = 100 }
                        ]
                    }
                ]
            },
            new StageProfile
            {
                Id = Guid.NewGuid(),
                Name = "Stage 5",
                StageIndex = 4,
                Weight = 100,
                Falloff = 0.12,
                Threshold = 1,
                Scenarios =
                [
                    new StageScenario
                    {
                        Id = Guid.NewGuid(),
                        EnemyCount = 4,
                        Weight = 100,
                        Slots =
                        [
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 1, ClassProfileId = twoHand, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 2, ClassProfileId = polearm, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 3, ClassProfileId = crossbow, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 4, ClassProfileId = archer, Weight = 100 }
                        ]
                    }
                ]
            },
            new StageProfile
            {
                Id = Guid.NewGuid(),
                Name = "Stage 6",
                StageIndex = 5,
                Weight = 100,
                Falloff = 0.10,
                Threshold = 1,
                Scenarios =
                [
                    new StageScenario
                    {
                        Id = Guid.NewGuid(),
                        EnemyCount = 4,
                        Weight = 100,
                        Slots =
                        [
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 1, ClassProfileId = twoHand, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 2, ClassProfileId = polearm, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 3, ClassProfileId = crossbow, Weight = 100 },
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 4, ClassProfileId = archer, Weight = 100 }
                        ]
                    }
                ]
            }
        ];
    }

    private static List<User> BuildUsers(Guid defaultProfileId)
    {
        var users = new List<User>();
        for (var i = 1; i <= 20; i++)
        {
            users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = $"Player{i}",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword($"player{i}"),
                Role = UserRole.Player,
                ProfileId = defaultProfileId,
                Currency = 900 + (i * 70),
                Equipment = new Equipment(),
                LastDailyReward = i == 1 ? DateTime.UtcNow.AddDays(-1) : null
            });
        }

        users[2].IsBlocked = true;
        users[2].BlockReason = "seed ban";
        users[2].BlockedUntil = DateTime.UtcNow.AddDays(2);

        users.AddRange(
        [
            new User { Id = Guid.NewGuid(), Username = "GameModerator", PasswordHash = BCrypt.Net.BCrypt.HashPassword("moderator"), Role = UserRole.GameModerator, ProfileId = defaultProfileId, Currency = 3000, Equipment = new Equipment() },
            new User { Id = Guid.NewGuid(), Username = "Admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"), Role = UserRole.Admin, ProfileId = defaultProfileId, Currency = 6000, Equipment = new Equipment() },
            new User { Id = Guid.NewGuid(), Username = "SuperAdmin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("superadmin"), Role = UserRole.SuperAdmin, ProfileId = defaultProfileId, Currency = 12000, Equipment = new Equipment() }
        ]);

        return users;
    }

    private static void SeedItemsAndInventories(AppDbContext context, IItemGenerationService generator, List<User> users)
    {
        var userItems = new Dictionary<Guid, List<Guid>>();
        var guaranteedArmorTypes = new[] { ArmorType.Helmet, ArmorType.Chestplate, ArmorType.Gauntlets, ArmorType.Greaves, ArmorType.Sabatons };
        var guaranteedWeaponTypes = new[] { WeaponType.Sword, WeaponType.Shortsword, WeaponType.Bow, WeaponType.Crossbow, WeaponType.Polearm, WeaponType.TwoHandSword };

        foreach (var user in users)
        {
            var itemIds = new List<Guid>();
            var collectedArmor = new HashSet<ArmorType>();
            var collectedWeapons = new HashSet<WeaponType>();

            void AddGeneratedItem(Item item)
            {
                switch (item)
                {
                    case Weapon w:
                        context.Weapons.Add(w);
                        collectedWeapons.Add(w.WeaponType);
                        break;
                    case Armor a:
                        context.Armors.Add(a);
                        collectedArmor.Add(a.ArmorType);
                        break;
                }

                context.InventoryItems.Add(new InventoryItem { Id = Guid.NewGuid(), UserId = user.Id, ItemId = item.Id });
                itemIds.Add(item.Id);
            }

            var safety = 0;
            while ((collectedArmor.Count < guaranteedArmorTypes.Length || collectedWeapons.Count < guaranteedWeaponTypes.Length) && safety < 260)
            {
                var item = generator.GenerateItemAsync(user.Id).Result;
                AddGeneratedItem(item);
                safety++;
            }

            for (var i = 0; i < 8; i++)
                AddGeneratedItem(generator.GenerateItemAsync(user.Id).Result);

            userItems[user.Id] = itemIds;
        }

        context.SaveChanges();

        foreach (var user in users)
        {
            var equipment = context.Equipments.First(x => x.UserId == user.Id);
            var ownedWeapons = context.Weapons
                .Where(x => userItems[user.Id].Contains(x.Id))
                .ToList();
            var ownedArmor = context.Armors.Where(x => userItems[user.Id].Contains(x.Id)).ToList();

            var distinctWeapons = ownedWeapons
                .GroupBy(x => x.WeaponType)
                .Select(x => x.First())
                .Take(4)
                .ToList();

            if (distinctWeapons.Count > 0) equipment.WeaponSlot1Id = distinctWeapons[0].Id;
            if (distinctWeapons.Count > 1) equipment.WeaponSlot2Id = distinctWeapons[1].Id;
            if (distinctWeapons.Count > 2) equipment.WeaponSlot3Id = distinctWeapons[2].Id;
            if (distinctWeapons.Count > 3) equipment.WeaponSlot4Id = distinctWeapons[3].Id;

            foreach (var armor in ownedArmor)
            {
                switch (armor.ArmorType)
                {
                    case ArmorType.Helmet: equipment.HeadId = armor.Id; break;
                    case ArmorType.Chestplate: equipment.BodyId = armor.Id; break;
                    case ArmorType.Gauntlets: equipment.GlovesId = armor.Id; break;
                    case ArmorType.Greaves: equipment.LegsId = armor.Id; break;
                    case ArmorType.Sabatons: equipment.BootsId = armor.Id; break;
                }
            }

            var runItemId = userItems[user.Id].First();
            context.RunInventoryItems.Add(new RunInventoryItem
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ItemId = runItemId
            });
        }

        context.SaveChanges();
    }

    private static void SeedMarketplace(AppDbContext context, List<User> users, DateTime now)
    {
        var rand = new Random(42);
        var playerUsers = users.Where(x => x.Role == UserRole.Player).ToList();
        var allInventory = context.InventoryItems.ToList();

        foreach (var seller in playerUsers)
        {
            var sellerItems = allInventory.Where(x => x.UserId == seller.Id).Take(4).ToList();
            foreach (var inv in sellerItems)
            {
                var category = context.Weapons.Any(x => x.Id == inv.ItemId) ? ItemCategory.Weapon : ItemCategory.Armor;
                var sold = rand.NextDouble() < 0.35;
                var price = rand.Next(80, 650);
                var listing = new MarketListing
                {
                    Id = Guid.NewGuid(),
                    SellerId = seller.Id,
                    ItemId = inv.ItemId,
                    Price = price,
                    Category = category,
                    CreatedAt = now.AddHours(-rand.Next(1, 72)),
                    IsSold = sold
                };
                context.MarketListings.Add(listing);

                if (!sold)
                {
                    context.MarketInventoryItems.Add(new MarketInventoryItem
                    {
                        Id = Guid.NewGuid(),
                        UserId = seller.Id,
                        ItemId = inv.ItemId
                    });
                }
                else
                {
                    var affordableBuyers = playerUsers
                        .Where(x => x.Id != seller.Id && x.Currency >= price)
                        .ToList();

                    if (affordableBuyers.Count == 0)
                    {
                        listing.IsSold = false;
                        context.MarketInventoryItems.Add(new MarketInventoryItem
                        {
                            Id = Guid.NewGuid(),
                            UserId = seller.Id,
                            ItemId = inv.ItemId
                        });
                        continue;
                    }

                    var buyer = affordableBuyers[rand.Next(affordableBuyers.Count)];
                    context.Transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyer.Id,
                        SellerId = seller.Id,
                        ItemId = inv.ItemId,
                        Price = price,
                        Timestamp = now.AddHours(-rand.Next(1, 48))
                    });

                    buyer.Currency -= price;
                    seller.Currency += price;
                }
            }
        }
    }

    private static void SeedChat(AppDbContext context, List<User> users, DateTime now)
    {
        var player1 = users.First(x => x.Username == "Player1");
        var player2 = users.First(x => x.Username == "Player2");
        var player3 = users.First(x => x.Username == "Player3");
        var mod = users.First(x => x.Username == "GameModerator");

        context.ChatMessages.AddRange(
            new ChatMessage { Id = Guid.NewGuid(), SenderId = player1.Id, Text = "Welcome to global market chat.", CreatedAt = now.AddMinutes(-140) },
            new ChatMessage { Id = Guid.NewGuid(), SenderId = player2.Id, Text = "Selling rare sword on market.", CreatedAt = now.AddMinutes(-120) },
            new ChatMessage { Id = Guid.NewGuid(), SenderId = mod.Id, Text = "Keep chat clean and respectful.", CreatedAt = now.AddMinutes(-110) },
            new ChatMessage { Id = Guid.NewGuid(), SenderId = player3.Id, Text = "Looking for chestplate with fire element.", CreatedAt = now.AddMinutes(-100) },

            new ChatMessage { Id = Guid.NewGuid(), SenderId = player1.Id, RecipientId = player2.Id, Text = "Hi, still selling that sword?", CreatedAt = now.AddMinutes(-90) },
            new ChatMessage { Id = Guid.NewGuid(), SenderId = player2.Id, RecipientId = player1.Id, Text = "Yes, listed on buy page.", CreatedAt = now.AddMinutes(-88) },
            new ChatMessage { Id = Guid.NewGuid(), SenderId = player1.Id, RecipientId = player2.Id, Text = "Great, I may buy it today.", CreatedAt = now.AddMinutes(-82) },

            new ChatMessage { Id = Guid.NewGuid(), SenderId = player1.Id, RecipientId = player3.Id, Text = "Need gauntlets or helmet?", CreatedAt = now.AddMinutes(-70) },
            new ChatMessage { Id = Guid.NewGuid(), SenderId = player3.Id, RecipientId = player1.Id, Text = "Mostly chestplate.", CreatedAt = now.AddMinutes(-65) },

            new ChatMessage { Id = Guid.NewGuid(), SenderId = player2.Id, RecipientId = mod.Id, Text = "Can you verify a suspicious listing?", CreatedAt = now.AddMinutes(-50) },
            new ChatMessage { Id = Guid.NewGuid(), SenderId = mod.Id, RecipientId = player2.Id, Text = "Checked, it is legitimate.", CreatedAt = now.AddMinutes(-45) }
        );
    }

    private static void SeedRuns(AppDbContext context, List<User> users, DateTime now)
    {
        var finishedRun = new Run
        {
            Id = Guid.NewGuid(),
            UserId = users[1].Id,
            Status = RunStatus.Returned,
            BattleIndex = 4,
            PlayerCurrentHp = 60,
            PlayerMaxHp = 100,
            StartedAt = now.AddDays(-1),
            FinishedAt = now.AddDays(-1).AddMinutes(25),
            Battles = new List<Battle>()
        };

        context.Runs.Add(finishedRun);
    }

    private static void SeedAdminArtifacts(AppDbContext context, List<User> users, DateTime now)
    {
        var admin = users.First(x => x.Role == UserRole.Admin);
        var target = users.First(x => x.Role == UserRole.Player);

        context.AdminLogs.Add(new AdminLog
        {
            Id = Guid.NewGuid(),
            AdminId = admin.Id,
            Action = "SEED_BOOTSTRAP",
            TargetUserId = target.Id.ToString(),
            CreatedAt = now,
            Data = "{\"source\":\"DbSeeder\"}"
        });
    }
}
