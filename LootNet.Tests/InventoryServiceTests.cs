namespace LootNet_API.Tests;

using LootNet_API.Data;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Services;
using LootNet_API.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class InventoryServiceTests
{
    private AppDbContext CreateDb()
        => TestDbContextFactory.Create();

    private async Task<User> SeedUser(AppDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "player",
            PasswordHash = "hash",
            Role = UserRole.Player,
            Currency = 1000,
            Equipment = new Equipment
            {
                UserId = Guid.NewGuid()
            }
        };

        db.Users.Add(user);

        var eq = new Equipment
        {
            Id = Guid.NewGuid(),
            UserId = user.Id
        };

        db.Equipments.Add(eq);

        await db.SaveChangesAsync();

        return user;
    }

    [Fact]
    public async Task GetItems_ReturnsWeaponsAndArmors()
    {
        var db = CreateDb();

        var user = await SeedUser(db);

        db.Weapons.Add(new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Sword",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Cut = 10,
            Blunt = 5
        });

        db.Armors.Add(new Armor
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Helmet",
            Category = ItemCategory.Armor,
            ArmorType = ArmorType.Head,
            CutResistance = 2,
            BluntResistance = 3
        });

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        var result = await service.GetItemsAsync(user.Id);

        Assert.Single(result.Weapons);
        Assert.Single(result.Armors);
    }

    [Fact]
    public async Task GetInventory_ExcludesEquippedItems()
    {
        var db = CreateDb();

        var user = await SeedUser(db);

        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Sword",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Cut = 10,
            Blunt = 5
        };

        db.Weapons.Add(weapon);

        var eq = await db.Equipments.FirstAsync(x => x.UserId == user.Id);
        eq.WeaponSlot1Id = weapon.Id;

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        var result = await service.GetInventoryAsync(user.Id);

        Assert.Empty(result.Weapons);
    }

    [Fact]
    public async Task GetEquipment_ReturnsMappedData()
    {
        var db = CreateDb();

        var user = await SeedUser(db);

        var armor = new Armor
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Helmet",
            Category = ItemCategory.Armor,
            ArmorType = ArmorType.Head,
            CutResistance = 2,
            BluntResistance = 3
        };

        db.Armors.Add(armor);

        var eq = await db.Equipments.FirstAsync(x => x.UserId == user.Id);
        eq.HeadId = armor.Id;

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        var result = await service.GetEquipmentAsync(user.Id);

        Assert.NotNull(result.Head);
        Assert.Equal("Helmet", result.Head!.Name);
    }

    [Fact]
    public async Task EquipWeapon_EquipsCorrectSlot()
    {
        var db = CreateDb();

        var user = await SeedUser(db);

        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Sword",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Cut = 10,
            Blunt = 5
        };

        db.Weapons.Add(weapon);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await service.EquipWeaponAsync(user.Id, weapon.Id, 2);

        var eq = await db.Equipments.FirstAsync(x => x.UserId == user.Id);

        Assert.Equal(weapon.Id, eq.WeaponSlot2Id);
    }

    [Fact]
    public async Task EquipArmor_EquipsByType()
    {
        var db = CreateDb();

        var user = await SeedUser(db);

        var armor = new Armor
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Helmet",
            Category = ItemCategory.Armor,
            ArmorType = ArmorType.Head,
            CutResistance = 2,
            BluntResistance = 3
        };

        db.Armors.Add(armor);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await service.EquipArmorAsync(user.Id, armor.Id);

        var eq = await db.Equipments.FirstAsync(x => x.UserId == user.Id);

        Assert.Equal(armor.Id, eq.HeadId);
    }

    [Fact]
    public async Task EquipWeapon_Throws_WhenInvalidSlot()
    {
        var db = CreateDb();

        var user = await SeedUser(db);

        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Sword",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Cut = 10,
            Blunt = 5
        };

        db.Weapons.Add(weapon);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EquipWeaponAsync(user.Id, weapon.Id, 999));
    }

    [Fact]
    public async Task UnequipItem_RemovesItemFromEquipment()
    {
        var db = CreateDb();

        var user = await SeedUser(db);

        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Sword",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Cut = 10,
            Blunt = 5
        };

        db.Weapons.Add(weapon);

        var eq = await db.Equipments.FirstAsync(x => x.UserId == user.Id);
        eq.WeaponSlot1Id = weapon.Id;

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await service.UnequipItemAsync(user.Id, weapon.Id);

        var updated = await db.Equipments.FirstAsync(x => x.UserId == user.Id);

        Assert.Null(updated.WeaponSlot1Id);
    }
    [Fact]
    public async Task EquipWeapon_OverwritesExistingWeaponInSlot()
    {
        var db = TestDbContextFactory.Create();

        var user = await SeedUser(db);

        var weapon1 = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Sword1",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Cut = 10,
            Blunt = 5
        };

        var weapon2 = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Sword2",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword,
            Cut = 15,
            Blunt = 7
        };

        db.Weapons.AddRange(weapon1, weapon2);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await service.EquipWeaponAsync(user.Id, weapon1.Id, 1);
        await service.EquipWeaponAsync(user.Id, weapon2.Id, 1);

        var eq = await db.Equipments.FirstAsync(x => x.UserId == user.Id);

        Assert.Equal(weapon2.Id, eq.WeaponSlot1Id);
    }
    [Fact]
    public async Task EquipArmor_OverwritesExistingArmor()
    {
        var db = TestDbContextFactory.Create();

        var user = await SeedUser(db);

        var armor1 = new Armor
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Helmet1",
            Category = ItemCategory.Armor,
            ArmorType = ArmorType.Head
        };

        var armor2 = new Armor
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Helmet2",
            Category = ItemCategory.Armor,
            ArmorType = ArmorType.Head
        };

        db.Armors.AddRange(armor1, armor2);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await service.EquipArmorAsync(user.Id, armor1.Id);
        await service.EquipArmorAsync(user.Id, armor2.Id);

        var eq = await db.Equipments.FirstAsync(x => x.UserId == user.Id);

        Assert.Equal(armor2.Id, eq.HeadId);
    }
    [Fact]
    public async Task EquipWeapon_Throws_WhenWeaponNotOwned()
    {
        var db = TestDbContextFactory.Create();

        var user1 = await SeedUser(db);
        var user2 = await SeedUser(db);

        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user2.Id,
            Name = "Sword",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword
        };

        db.Weapons.Add(weapon);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EquipWeaponAsync(user1.Id, weapon.Id, 1));
    }
    [Fact]
    public async Task EquipArmor_Throws_WhenArmorNotOwned()
    {
        var db = TestDbContextFactory.Create();

        var user1 = await SeedUser(db);
        var user2 = await SeedUser(db);

        var armor = new Armor
        {
            Id = Guid.NewGuid(),
            OwnerId = user2.Id,
            Name = "Helmet",
            Category = ItemCategory.Armor,
            ArmorType = ArmorType.Head
        };

        db.Armors.Add(armor);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EquipArmorAsync(user1.Id, armor.Id));
    }
    [Fact]
    public async Task GetInventory_ReturnsAllItems_WhenNothingEquipped()
    {
        var db = TestDbContextFactory.Create();

        var user = await SeedUser(db);

        db.Weapons.Add(new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Sword",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword
        });

        db.Armors.Add(new Armor
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Helmet",
            Category = ItemCategory.Armor,
            ArmorType = ArmorType.Head
        });

        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        var result = await service.GetInventoryAsync(user.Id);

        Assert.Single(result.Weapons);
        Assert.Single(result.Armors);
    }
    [Fact]
    public async Task UnequipItem_DoesNothing_WhenItemNotEquipped()
    {
        var db = TestDbContextFactory.Create();

        var user = await SeedUser(db);

        var weapon = new Weapon
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Name = "Sword",
            Category = ItemCategory.Weapon,
            WeaponType = WeaponType.Sword
        };

        db.Weapons.Add(weapon);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);

        await service.UnequipItemAsync(user.Id, weapon.Id);

        var eq = await db.Equipments.FirstAsync(x => x.UserId == user.Id);

        Assert.Null(eq.WeaponSlot1Id);
        Assert.Null(eq.WeaponSlot2Id);
    }
}