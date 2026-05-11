using LootNet_API.Data;
using LootNet_API.DTO.GameRun;
using LootNet_API.Enums;
using LootNet_API.Models.GameRun;
using LootNet_API.Services;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class GameRunService : IGameRunService
{
    private const int PlayerStartHp = 100;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly BattleService _battle;
    private readonly IEnemyGenerationService _enemyGeneration;
    private readonly IInventoryService _inventory;
    private readonly IRealtimeNotifier? _realtimeNotifier;

    public GameRunService(
        IDbContextFactory<AppDbContext> dbFactory,
        BattleService battle,
        IEnemyGenerationService enemyGeneration,
        IInventoryService inventory,
        IRealtimeNotifier? realtimeNotifier = null)
    {
        _dbFactory = dbFactory;
        _battle = battle;
        _enemyGeneration = enemyGeneration;
        _inventory = inventory;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<RunDTO?> GetActiveRunAsync(Guid userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var run = await db.Runs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId &&
                (x.Status == RunStatus.Active || x.Status == RunStatus.InBattle));

        return run == null ? null : MapRun(run);
    }

    public async Task<BattleDTO?> GetCurrentBattleAsync(Guid userId)
    {
        await using var db = _dbFactory.CreateDbContext();
        var run = await db.Runs
            .AsNoTracking()
            .Include(x => x.Battles)
                .ThenInclude(x => x.Enemies)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == RunStatus.InBattle);

        if (run == null)
            return null;

        var battle = run.Battles.OrderByDescending(x => x.Id).FirstOrDefault();
        if (battle == null)
            return null;

        return MapBattle(run, battle);
    }

    public async Task<RunDTO> StartRunAsync(Guid userId, StartRunDTO dto)
    {
        await using var db = _dbFactory.CreateDbContext();

        var existing = await db.Runs
            .FirstOrDefaultAsync(x => x.UserId == userId &&
                (x.Status == RunStatus.Active || x.Status == RunStatus.InBattle));

        if (existing != null)
            return MapRun(existing);

        await _inventory.MoveToRunAsync(userId, dto.ItemIds);

        var run = new Run
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = RunStatus.Active,
            BattleIndex = 0,
            PlayerCurrentHp = PlayerStartHp,
            PlayerMaxHp = PlayerStartHp,
            StartedAt = DateTime.UtcNow,
            Battles = new List<Battle>()
        };

        db.Runs.Add(run);
        await db.SaveChangesAsync();
        await NotifyAsync("run", "started", userId, new { runId = run.Id });

        return MapRun(run);
    }

    public async Task<BattleDTO> GoFurtherAsync(Guid userId)
    {
        await using var db = _dbFactory.CreateDbContext();

        var run = await GetRunAsync(db, userId, RunStatus.Active);
        var enemies = await _enemyGeneration.GenerateEnemiesAsync(run.BattleIndex);

        var battle = new Battle
        {
            Id = Guid.NewGuid(),
            RunId = run.Id,
            Enemies = enemies
        };

        run.Battles.Add(battle);
        await _battle.StartBattleAsync(run, battle);
        db.ChangeTracker.TrackGraph(battle, node =>
        {
            if (node.Entry.State == EntityState.Detached)
                node.Entry.State = EntityState.Added;
        });
        await db.SaveChangesAsync();
        await NotifyAsync("run", "go-further", userId, new { runId = run.Id, battleId = battle.Id });

        return MapBattle(run, battle);
    }

    public async Task<BattleResultDTO> FinishTurnAsync(Guid userId, FinishTurnDTO dto)
    {
        await using var db = _dbFactory.CreateDbContext();

        var run = await GetRunAsync(db, userId, RunStatus.InBattle);

        var battle = run.Battles.FirstOrDefault(x => x.Id == dto.BattleId)
            ?? throw new InvalidOperationException("Battle not found");

        var result = await _battle.HandlePlayerTurnAsync(run, battle, dto.Action);

        if (run.Status == RunStatus.Lost)
            await _inventory.LoseRunItemsAsync(userId);

        await db.SaveChangesAsync();
        await NotifyAsync("run", "finish-turn", userId, new { runId = run.Id, battleId = battle.Id, run.Status });

        return result;
    }

    public async Task<RunDTO> EndRunAsync(Guid userId)
    {
        await using var db = _dbFactory.CreateDbContext();

        var run = await GetRunAsync(db, userId, RunStatus.Active);

        run.Status = RunStatus.Returned;
        run.FinishedAt = DateTime.UtcNow;

        await _inventory.ReturnFromRunAsync(userId);
        await db.SaveChangesAsync();
        await NotifyAsync("run", "ended", userId, new { runId = run.Id, status = run.Status });

        return MapRun(run);
    }

    public async Task<RunDTO> ForceReturnAsync(Guid userId)
    {
        await using var db = _dbFactory.CreateDbContext();

        var run = await db.Runs
            .Include(x => x.Battles)
                .ThenInclude(x => x.Enemies)
            .FirstOrDefaultAsync(x => x.UserId == userId &&
                (x.Status == RunStatus.Active || x.Status == RunStatus.InBattle));

        if (run == null)
            throw new InvalidOperationException("No active run found");

        run.Status = RunStatus.ForcedReturn;
        run.FinishedAt = DateTime.UtcNow;

        await _inventory.ReturnFromRunAsync(userId);
        await db.SaveChangesAsync();
        await NotifyAsync("run", "forced-return", userId, new { runId = run.Id });

        return MapRun(run);
    }

    private static async Task<Run> GetRunAsync(AppDbContext db, Guid userId, RunStatus requiredStatus)
    {
        var run = await db.Runs
            .Include(x => x.Battles)
                .ThenInclude(x => x.Enemies)
                    .ThenInclude(x => x.Equipment)
            .FirstOrDefaultAsync(x => x.UserId == userId &&
                (x.Status == RunStatus.Active || x.Status == RunStatus.InBattle));

        if (run == null)
            throw new InvalidOperationException("No active run found");

        if (run.Status != requiredStatus)
            throw new InvalidOperationException($"Run must be in {requiredStatus} status");

        return run;
    }

    private static RunDTO MapRun(Run run) => new()
    {
        Id = run.Id,
        Status = run.Status,
        BattleIndex = run.BattleIndex,
        PlayerCurrentHp = run.PlayerCurrentHp,
        PlayerMaxHp = run.PlayerMaxHp
    };

    private static BattleDTO MapBattle(Run run, Battle battle) => new()
    {
        BattleId = battle.Id,
        RunId = run.Id,
        PlayerCurrentHp = run.PlayerCurrentHp,
        PlayerMaxHp = run.PlayerMaxHp,
        PlayerPosition = run.PlayerPosition,
        LeftHandItemId = run.LeftHandItemId,
        RightHandItemId = run.RightHandItemId,
        Enemies = battle.Enemies.Select(e => new BattleEnemyDTO
        {
            Id = e.Id,
            Position = e.Position,
            CurrentHp = e.CurrentHp,
            MaxHp = e.MaxHp,
            LeftHandItemId = e.LeftHandItemId,
            RightHandItemId = e.RightHandItemId
        }).ToList()
    };

    private Task NotifyAsync(string domain, string action, Guid userId, object? data = null)
        => _realtimeNotifier?.AppChangedAsync(domain, action, userId, data) ?? Task.CompletedTask;
}
