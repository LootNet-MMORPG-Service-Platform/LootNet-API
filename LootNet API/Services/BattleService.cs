using LootNet_API.DTO.GameRun;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.GameRun;
using LootNet_API.Models.Items;
using LootNet_API.Services.Interfaces;

namespace LootNet_API.Services;

public class BattleService
{
    private const int PlayerStartPosition = 0;
    private const int MinPlayerPosition = 0;
    private const int MaxPlayerPosition = 3;

    private readonly CombatHandsService _hands;
    private readonly DamageCalculator _damage;
    private readonly IEquipmentService _equipment;

    public BattleService(
        CombatHandsService hands,
        DamageCalculator damage,
        IEquipmentService equipment)
    {
        _hands = hands;
        _damage = damage;
        _equipment = equipment;
    }

    public async Task<Battle> StartBattleAsync(Run run, Battle battle)
    {
        run.PlayerPosition = PlayerStartPosition;

        var playerWeapons = await LoadPlayerWeaponsAsync(run);
        _hands.InitializePlayerHands(run, playerWeapons);

        foreach (var enemy in battle.Enemies)
        {
            var enemyWeapons = await LoadEnemyWeaponsAsync(enemy);
            _hands.InitializeEnemyHands(enemy, enemyWeapons);
        }

        run.Status = RunStatus.InBattle;

        return battle;
    }

    public async Task<BattleResultDTO> HandlePlayerTurnAsync(Run run,Battle battle,TurnActionDTO action)
    {
        var result = new BattleResultDTO { Log = new List<string>() };

        if (run.PlayerCurrentHp <= 0)
            return EndRun(run, RunStatus.Lost, result);

        if (run.PlayerSkipNextTurn)
        {
            run.PlayerSkipNextTurn = false;
            await EnemyTurnAsync(run, battle, result);
            return await CheckBattleEndAsync(run, battle, result);
        }

        await HandleActionAsync(run, battle, action, result);

        if (result.RunFinished)
            return result;

        if (!battle.Enemies.Any())
            return AdvanceBattle(run, result);

        await EnemyTurnAsync(run, battle, result);

        return await CheckBattleEndAsync(run, battle, result);
    }

    private async Task HandleActionAsync(Run run,Battle battle,
        TurnActionDTO action,BattleResultDTO result)
    {
        switch (action.Type)
        {
            case ActionType.Attack:
                await PlayerAttackAsync(run, battle, action, result);
                break;

            case ActionType.ChangeEquipment:
                _hands.ChangePlayerHands(run, action.LeftWeapon, action.RightWeapon);
                run.IsPlayerDisorganized = true;
                result.Log.Add("Player changed equipment");
                break;

            case ActionType.ChangePosition:
                run.PlayerPosition = Clamp(action.TargetPosition, MinPlayerPosition, MaxPlayerPosition);
                run.IsPlayerDisorganized = true;
                result.Log.Add($"Player moved to position {run.PlayerPosition}");
                break;

            case ActionType.SkipTurn:
                result.PlayerSkipped = true;
                result.Log.Add("Player skipped turn");
                break;
        }
    }

    private async Task PlayerAttackAsync(Run run,Battle battle,TurnActionDTO action,BattleResultDTO result)
    {
        var target = battle.Enemies.FirstOrDefault(e => e.Position == action.TargetPosition);
        if (target == null)
            return;

        var isRanged = await PlayerIsUsingRangedAsync(run);

        if (!isRanged && !IsAdjacent(run.PlayerPosition, target.Position))
            return;

        var playerEquipment = await LoadPlayerEquipmentAsync(run.UserId);

        var damage = await _damage.CalculatePlayerToEnemyAsync(run, playerEquipment, target, isRanged);

        target.CurrentHp -= damage;
        result.DamageDealt += damage;
        result.Log.Add($"Player hit enemy {target.Id} for {damage}");

        if (target.CurrentHp <= 0)
        {
            battle.Enemies.Remove(target);
            result.Log.Add($"Enemy {target.Id} defeated");
        }

        run.IsPlayerDisorganized = false;
    }

    private async Task EnemyTurnAsync(Run run,Battle battle,BattleResultDTO result)
    {
        var playerEquipment = await LoadPlayerEquipmentAsync(run.UserId);

        foreach (var enemy in battle.Enemies.ToList())
        {
            if (enemy.CurrentHp <= 0)
                continue;

            if (enemy.SkipNextTurn)
            {
                enemy.SkipNextTurn = false;
                continue;
            }

            var weapons = await LoadEnemyWeaponsAsync(enemy);
            var weapon = GetActiveWeapon(enemy, weapons);

            if (weapon == null)
                continue;

            var isRanged = weapon.WeaponType.IsRanged();

            if (!isRanged && !IsAdjacent(enemy.Position, run.PlayerPosition))
            {
                enemy.Position = Math.Max(1, enemy.Position - 1);
                enemy.IsDisorganized = true;
                result.Log.Add($"Enemy {enemy.Id} moved to position {enemy.Position}");
                continue;
            }

            var damage = await _damage.CalculateEnemyToPlayerAsync(enemy, run, playerEquipment, isRanged);

            run.PlayerCurrentHp -= damage;
            result.EnemyDamage += damage;
            result.Log.Add($"Enemy {enemy.Id} hit player for {damage}");

            enemy.IsDisorganized = false;
        }
    }

    private async Task<bool> PlayerIsUsingRangedAsync(Run run)
    {
        var left = await _equipment.GetWeapon(run.LeftHandItemId);
        if (left != null && left.WeaponType.IsRanged())
            return true;

        var rightId = run.RightHandItemId != run.LeftHandItemId ? run.RightHandItemId : null;
        var right = await _equipment.GetWeapon(rightId);
        if (right != null && right.WeaponType.IsRanged())
            return true;

        return false;
    }

    private async Task<Equipment> LoadPlayerEquipmentAsync(Guid userId)
    {
        return await _equipment.GetEquipmentModelAsync(userId)
            ?? new Equipment { UserId = userId };
    }

    private async Task<List<Weapon>> LoadPlayerWeaponsAsync(Run run)
    {
        var slots = new[] { run.LeftHandItemId, run.RightHandItemId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();

        var weapons = new List<Weapon>();
        foreach (var id in slots)
        {
            var w = await _equipment.GetWeaponModelAsync(id);
            if (w != null)
                weapons.Add(w);
        }

        return weapons;
    }

    private async Task<List<Weapon>> LoadEnemyWeaponsAsync(BattleEnemy enemy)
    {
        if (enemy.Equipment == null)
            return new List<Weapon>();
        var slots = new[]
        {
            enemy.Equipment.WeaponSlot1Id,
            enemy.Equipment.WeaponSlot2Id,
            enemy.Equipment.WeaponSlot3Id,
            enemy.Equipment.WeaponSlot4Id
        }
        .Where(id => id.HasValue)
        .Select(id => id!.Value)
        .Distinct();

        var weapons = new List<Weapon>();
        foreach (var id in slots)
        {
            var w = await _equipment.GetWeaponModelAsync(id);
            if (w != null)
                weapons.Add(w);
        }

        return weapons;
    }

    private static Weapon? GetActiveWeapon(BattleEnemy enemy, List<Weapon> weapons)
    {
        var left = weapons.FirstOrDefault(w => w.Id == enemy.LeftHandItemId);
        var right = weapons.FirstOrDefault(w => w.Id == enemy.RightHandItemId);
        return left ?? right;
    }

    private static bool IsAdjacent(int a, int b)
        => Math.Abs(a - b) == 1;

    private static int Clamp(int value, int min, int max)
        => Math.Max(min, Math.Min(max, value));

    private static BattleResultDTO EndRun(Run run, RunStatus status, BattleResultDTO result)
    {
        run.Status = status;
        result.RunFinished = true;
        return result;
    }

    private static BattleResultDTO AdvanceBattle(Run run, BattleResultDTO result)
    {
        run.Status = RunStatus.Active;
        run.BattleIndex++;
        result.RunFinished = true;
        return result;
    }

    private static async Task<BattleResultDTO> CheckBattleEndAsync(Run run, Battle battle, BattleResultDTO result)
    {
        if (run.PlayerCurrentHp <= 0)
            return EndRun(run, RunStatus.Lost, result);

        if (!battle.Enemies.Any())
            return AdvanceBattle(run, result);

        return result;
    }
}