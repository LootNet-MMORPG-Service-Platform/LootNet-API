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
        var eliteProfile = BuildEliteGenerationProfile();

        context.GenerationProfiles.AddRange(defaultProfile, eliteProfile);
        context.SaveChanges();

        var classProfiles = BuildEnemyClassProfiles(defaultProfile.Id, eliteProfile.Id);
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
        SeedRuns(context, users, classProfiles, now);
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
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Sword, Weight = 45 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Shortsword, Weight = 25 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Weapon, WeaponType = WeaponType.Bow, Weight = 15 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Chestplate, Weight = 30 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Helmet, Weight = 20 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Greaves, Weight = 20 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Sabatons, Weight = 15 },
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Gauntlets, Weight = 15 }
        ]);

        profile.Rules.AddRange(
        [
            CreateWeaponRule(profile.Id, WeaponType.Sword, false, 10, 26, 4, 16),
            CreateWeaponRule(profile.Id, WeaponType.Shortsword, false, 12, 28, 2, 14),
            CreateWeaponRule(profile.Id, WeaponType.Bow, false, 8, 18, 1, 8),
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
            new ItemTypeWeight { Id = Guid.NewGuid(), ProfileId = profile.Id, Category = ItemCategory.Armor, ArmorType = ArmorType.Chestplate, Weight = 25 }
        ]);

        profile.Rules.AddRange(
        [
            CreateWeaponRule(profile.Id, WeaponType.TwoHandSword, false, 18, 38, 8, 22),
            CreateWeaponRule(profile.Id, WeaponType.Polearm, false, 16, 34, 7, 20),
            CreateWeaponRule(profile.Id, WeaponType.TwoHandSword, true, 8, 16, 3, 10),
            CreateArmorRule(profile.Id, ArmorType.Chestplate, false, 14, 28, 6, 16),
            CreateArmorRule(profile.Id, ArmorType.Chestplate, true, 7, 14, 3, 8)
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

    private static List<EnemyClassProfile> BuildEnemyClassProfiles(Guid defaultProfileId, Guid eliteProfileId)
    {
        return
        [
            new EnemyClassProfile { Id = Guid.NewGuid(), Name = "Front Tank", Class = EnemyClass.Tank, AllowedColumns = [1], GenerationProfileId = eliteProfileId, Weight = 20 },
            new EnemyClassProfile { Id = Guid.NewGuid(), Name = "Halberd Guard", Class = EnemyClass.Polearm, AllowedColumns = [1,2], GenerationProfileId = eliteProfileId, Weight = 18 },
            new EnemyClassProfile { Id = Guid.NewGuid(), Name = "Skirmisher", Class = EnemyClass.Skirmisher, AllowedColumns = [1,2], GenerationProfileId = defaultProfileId, Weight = 20 },
            new EnemyClassProfile { Id = Guid.NewGuid(), Name = "Crossbowman", Class = EnemyClass.Crossbow, AllowedColumns = [2,3], GenerationProfileId = defaultProfileId, Weight = 16 },
            new EnemyClassProfile { Id = Guid.NewGuid(), Name = "Archer", Class = EnemyClass.Archer, AllowedColumns = [3,4], GenerationProfileId = defaultProfileId, Weight = 14 },
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
                            new ScenarioSlot { Id = Guid.NewGuid(), Position = 1, ClassProfileId = polearm, Weight = 100 },
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
            }
        ];
    }

    private static List<User> BuildUsers(Guid defaultProfileId)
    {
        return
        [
            new User { Id = Guid.NewGuid(), Username = "Player1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("player1"), Role = UserRole.Player, ProfileId = defaultProfileId, Currency = 1500, Equipment = new Equipment(), LastDailyReward = DateTime.UtcNow.AddDays(-1) },
            new User { Id = Guid.NewGuid(), Username = "Player2", PasswordHash = BCrypt.Net.BCrypt.HashPassword("player2"), Role = UserRole.Player, ProfileId = defaultProfileId, Currency = 1300, Equipment = new Equipment() },
            new User { Id = Guid.NewGuid(), Username = "Player3", PasswordHash = BCrypt.Net.BCrypt.HashPassword("player3"), Role = UserRole.Player, ProfileId = defaultProfileId, Currency = 900, Equipment = new Equipment(), IsBlocked = true, BlockReason = "seed ban", BlockedUntil = DateTime.UtcNow.AddDays(2) },
            new User { Id = Guid.NewGuid(), Username = "GameModerator", PasswordHash = BCrypt.Net.BCrypt.HashPassword("moderator"), Role = UserRole.GameModerator, ProfileId = defaultProfileId, Currency = 2000, Equipment = new Equipment() },
            new User { Id = Guid.NewGuid(), Username = "Admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"), Role = UserRole.Admin, ProfileId = defaultProfileId, Currency = 5000, Equipment = new Equipment() },
            new User { Id = Guid.NewGuid(), Username = "SuperAdmin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("superadmin"), Role = UserRole.SuperAdmin, ProfileId = defaultProfileId, Currency = 10000, Equipment = new Equipment() }
        ];
    }

    private static void SeedItemsAndInventories(AppDbContext context, IItemGenerationService generator, List<User> users)
    {
        var userItems = new Dictionary<Guid, List<Guid>>();

        foreach (var user in users)
        {
            var itemIds = new List<Guid>();

            for (var i = 0; i < 8; i++)
            {
                var item = generator.GenerateItemAsync(user.Id).Result;

                if (item is Weapon w)
                {
                    context.Weapons.Add(w);
                }
                else if (item is Armor a)
                {
                    context.Armors.Add(a);
                }

                context.InventoryItems.Add(new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ItemId = item.Id
                });

                itemIds.Add(item.Id);
            }

            userItems[user.Id] = itemIds;
        }

        context.SaveChanges();

        foreach (var user in users)
        {
            var equipment = context.Equipments.First(x => x.UserId == user.Id);
            var ownedWeapon = context.Weapons.FirstOrDefault(x => userItems[user.Id].Contains(x.Id));
            var ownedArmor = context.Armors.Where(x => userItems[user.Id].Contains(x.Id)).ToList();

            if (ownedWeapon != null)
                equipment.WeaponSlot1Id = ownedWeapon.Id;

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
                    var buyer = playerUsers.First(x => x.Id != seller.Id);
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

    private static void SeedRuns(AppDbContext context, List<User> users, List<EnemyClassProfile> classProfiles, DateTime now)
    {
        var runner = users[0];

        var run = new Run
        {
            Id = Guid.NewGuid(),
            UserId = runner.Id,
            Status = RunStatus.InBattle,
            BattleIndex = 2,
            PlayerCurrentHp = 84,
            PlayerMaxHp = 100,
            PlayerPosition = 2,
            StartedAt = now.AddMinutes(-40),
            Battles = new List<Battle>()
        };

        var battle = new Battle
        {
            Id = Guid.NewGuid(),
            RunId = run.Id,
            Enemies = new List<BattleEnemy>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Class = classProfiles.First().Class,
                    Position = 1,
                    CurrentHp = 90,
                    MaxHp = 100,
                    Equipment = new Equipment()
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Class = classProfiles.Skip(1).First().Class,
                    Position = 3,
                    CurrentHp = 70,
                    MaxHp = 100,
                    Equipment = new Equipment()
                }
            }
        };

        run.Battles.Add(battle);

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

        context.Runs.AddRange(run, finishedRun);
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
