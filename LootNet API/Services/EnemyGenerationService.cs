using LootNet_API.Data;
using LootNet_API.Enums;
using LootNet_API.Models.GameRun;
using LootNet_API.Models.GameRun.EnemyGeneration;
using LootNet_API.Models.Items;
using LootNet_API.Models.Items.Generation;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LootNet_API.Services;



public class EnemyGenerationService : IEnemyGenerationService
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
        var generatedItems = new List<Item>();

        foreach (var slot in scenario.Slots)
        {
            var profile = await _context.Set<EnemyClassProfile>()
                .FirstOrDefaultAsync(x => x.Id == slot.ClassProfileId);

            if (profile == null)
                continue;

            if (!profile.AllowedColumns.Contains(slot.Position))
                continue;

            var enemy = await CreateEnemy(profile, slot.Position, generatedItems);

            enemies.Add(enemy);

            if (enemies.Count >= 4)
                break;
        }

        if (enemies.Count == 0)
        {
            var fallbackProfile = await _context.Set<EnemyClassProfile>()
                .OrderByDescending(x => x.Weight)
                .FirstOrDefaultAsync();

            if (fallbackProfile != null)
            {
                var fallbackEnemy = await CreateEnemy(fallbackProfile, 1, generatedItems);
                enemies.Add(fallbackEnemy);
            }
        }

        if (generatedItems.Count > 0)
        {
            var weapons = generatedItems.OfType<Weapon>().ToList();
            var armors = generatedItems.OfType<Armor>().ToList();

            if (weapons.Count > 0)
                _context.Weapons.AddRange(weapons);
            if (armors.Count > 0)
                _context.Armors.AddRange(armors);

            await _context.SaveChangesAsync();
        }

        return enemies;
    }

    private async Task<BattleEnemy> CreateEnemy(EnemyClassProfile profile, int position, List<Item> generatedItems)
    {
        var items = await _itemGenerationService.GenerateForEnemyAsync(profile.GenerationProfileId);
        generatedItems.AddRange(items);

        var equipment = new Equipment();

        _equipmentService.ApplyEnemyEquipment(equipment, items);

        return new BattleEnemy
        {
            Id = Guid.NewGuid(),
            Position = position,
            Class = profile.Class,
            CurrentHp = CurrentHP,
            MaxHp = MaxHP,
            Equipment = equipment
        };
    }

    private async Task<StageProfile?> GetStage(int index)
    {
        var exact = await _context.Set<StageProfile>()
            .Include(x => x.Scenarios)
                .ThenInclude(x => x.Slots)
            .FirstOrDefaultAsync(x => x.StageIndex == index);

        if (exact != null)
            return exact;

        var fallback = await _context.Set<StageProfile>()
            .Include(x => x.Scenarios)
                .ThenInclude(x => x.Slots)
            .Where(x => x.StageIndex <= index)
            .OrderByDescending(x => x.StageIndex)
            .FirstOrDefaultAsync();

        if (fallback == null)
            return null;

        var candidates = await _context.Set<StageProfile>()
            .Include(x => x.Scenarios)
                .ThenInclude(x => x.Slots)
            .Where(x => x.StageIndex <= fallback.StageIndex && x.StageIndex >= Math.Max(0, fallback.StageIndex - fallback.Threshold))
            .ToListAsync();

        if (candidates.Count <= 1)
            return fallback;

        var weighted = new List<(StageProfile stage, double w)>();
        foreach (var c in candidates)
        {
            var distance = fallback.StageIndex - c.StageIndex;
            var decay = Math.Pow(1.0 - Math.Clamp(fallback.Falloff, 0.0, 0.95), distance);
            weighted.Add((c, Math.Max(0.0001, c.Weight * decay)));
        }

        var total = weighted.Sum(x => x.w);
        var roll = Random.Shared.NextDouble() * total;
        var acc = 0.0;
        foreach (var item in weighted.OrderByDescending(x => x.stage.StageIndex))
        {
            acc += item.w;
            if (roll <= acc)
                return item.stage;
        }

        return fallback;
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
