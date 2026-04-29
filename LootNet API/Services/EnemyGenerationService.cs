using LootNet_API.Data;
using LootNet_API.Enums;
using LootNet_API.Models.GameRun;
using LootNet_API.Models.GameRun.EnemyGeneration;
using LootNet_API.Models.Items.Generation;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LootNet_API.Services;



public class EnemyGenerationService
{
    private readonly AppDbContext _context;
    private readonly IItemGenerationService _itemGenerationService;
    private readonly IEquipmentService _equipmentService;

    private const double WeightScale = 1000.0;
    private const int CurrentHP = 100;
    private const int MaxHP = 100;

    public EnemyGenerationService(
        AppDbContext context,
        IItemGenerationService itemGenerationService,
        IEquipmentService equipmentService)
    {
        _context = context;
        _itemGenerationService = itemGenerationService;
        _equipmentService = equipmentService;
    }

    public async Task<List<BattleEnemy>> GenerateEnemiesAsync(int stageIndex)
    {
        var stage = await GetStage(stageIndex);

        if (stage == null || stage.Scenarios.Count == 0)
            throw new InvalidOperationException("No stage scenarios defined");

        var scenario = PickWeighted(stage.Scenarios);

        if (scenario == null)
            throw new InvalidOperationException("No valid scenario");

        var enemies = new List<BattleEnemy>();

        foreach (var slot in scenario.Slots)
        {
            var profile = await _context.Set<EnemyClassProfile>()
                .FirstOrDefaultAsync(x => x.Id == slot.ClassProfileId);

            if (profile == null)
                continue;

            if (!CanBePlaced(profile.Class, slot.Position))
                continue;

            var enemy = await CreateEnemy(profile, slot.Position);

            enemies.Add(enemy);

            if (enemies.Count >= 4)
                break;
        }

        return enemies;
    }

    private async Task<BattleEnemy> CreateEnemy(EnemyClassProfile profile, int position)
    {
        var items = await _itemGenerationService.GenerateForEnemyAsync(profile.GenerationProfileId);

        var equipment = new Equipment();

        _equipmentService.ApplyEnemyEquipment(equipment, items);

        return new BattleEnemy
        {
            Id = Guid.NewGuid(),
            Position = position,
            CurrentHp = CurrentHP,
            MaxHp = MaxHP,
            Equipment = equipment
        };
    }

    private bool CanBePlaced(EnemyClass enemyClass, int position)
    {
        return enemyClass switch
        {
            EnemyClass.Tank => position == 1,
            EnemyClass.Polearm => position is 1 or 2,
            EnemyClass.Skirmisher => position is 1 or 2,
            EnemyClass.Crossbow => position is 2 or 3,
            EnemyClass.Archer => position is 3 or 4,
            EnemyClass.TwoHand => position == 1,
            EnemyClass.DualWield => position == 1,
            _ => false
        };
    }

    private async Task<StageProfile?> GetStage(int index)
    {
        return await _context.Set<StageProfile>()
            .Include(x => x.Scenarios)
                .ThenInclude(x => x.Slots)
            .FirstOrDefaultAsync(x => x.StageIndex == index);
    }

    private T? PickWeighted<T>(List<T> items) where T : class
    {
        var total = items.Sum(x =>
            (double)x!.GetType().GetProperty("Weight")!.GetValue(x)!);

        var roll = Random.Shared.NextDouble() * total;
        var current = 0.0;

        foreach (var item in items)
        {
            var weight = (double)item!.GetType().GetProperty("Weight")!.GetValue(item)!;

            current += weight;

            if (roll <= current)
                return item;
        }

        return items.FirstOrDefault();
    }
}