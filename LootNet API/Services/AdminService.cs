using LootNet_API.Data;
using LootNet_API.Configuration;
using LootNet_API.DTO;
using LootNet_API.DTO.Admin;
using LootNet_API.DTO.Items;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.GameRun;
using LootNet_API.Models.Logs;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace LootNet_API.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly IInventoryService _inventory;
    private readonly IEquipmentService _equipment;
    private readonly MarketplaceEconomyOptions _economyOptions;
    private readonly IRealtimeNotifier? _realtimeNotifier;

    public AdminService(
        AppDbContext context,
        IInventoryService inventory,
        IEquipmentService equipment,
        IRealtimeNotifier? realtimeNotifier = null,
        IOptions<MarketplaceEconomyOptions>? economyOptions = null)
    {
        _context = context;
        _inventory = inventory;
        _equipment = equipment;
        _economyOptions = economyOptions?.Value ?? new MarketplaceEconomyOptions();
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<PagedResultDTO<AdminUserListDTO>> GetUsersAsync(GetUsersQueryDTO query)
    {
        var q = _context.Users.AsQueryable();

        q = ApplyUserFilters(q, query);
        q = ApplyUserSorting(q, query.SortBy, query.SortDir);

        var total = await q.CountAsync();

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AdminUserListDTO
            {
                Id = x.Id,
                Username = x.Username,
                Role = x.Role,
                Currency = x.Currency,
                IsBlocked = x.IsBlocked
            })
            .ToListAsync();

        return new PagedResultDTO<AdminUserListDTO>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<AdminUserDetailsDTO> GetUserAsync(Guid id)
    {
        var user = await _context.Users.FirstAsync(x => x.Id == id);

        return new AdminUserDetailsDTO
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            Currency = user.Currency,
            IsBlocked = user.IsBlocked,
            BlockedUntil = user.BlockedUntil,
            BlockReason = user.BlockReason
        };
    }

    public async Task<ItemCollectionDTO> GetUserInventoryAsync(Guid userId)
    {
        return await _inventory.GetInventoryAsync(userId);
    }

    public async Task<ItemCollectionDTO> GetUserRunInventoryAsync(Guid userId)
    {
        return await _inventory.GetRunInventoryAsync(userId);
    }

    public async Task<ItemCollectionDTO> GetUserMarketInventoryAsync(Guid userId)
    {
        return await _inventory.GetMarketInventoryAsync(userId);
    }

    public async Task<EquipmentResponseDTO> GetUserEquipmentAsync(Guid userId)
    {
        return await _equipment.GetEquipmentAsync(userId);
    }

    public async Task<PagedResultDTO<AdminRunListDTO>> GetRunsAsync(GetRunsQueryDTO query)
    {
        var q = _context.Runs.AsQueryable();

        if (query.UserId.HasValue)
            q = q.Where(x => x.UserId == query.UserId.Value);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(x => x.StartedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(MapRunList())
            .ToListAsync();

        return new PagedResultDTO<AdminRunListDTO>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<List<AdminRunListDTO>> GetUserRunsAsync(Guid userId)
    {
        return await _context.Runs
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.StartedAt)
            .Select(MapRunList())
            .ToListAsync();
    }

    public async Task<AdminRunDetailsDTO> GetRunAsync(Guid runId)
    {
        return await _context.Runs
            .Where(x => x.Id == runId)
            .Select(x => new AdminRunDetailsDTO
            {
                Id = x.Id,
                UserId = x.UserId,
                Status = x.Status,
                BattleIndex = x.BattleIndex,
                PlayerCurrentHp = x.PlayerCurrentHp,
                PlayerMaxHp = x.PlayerMaxHp,
                IsPlayerDisorganized = x.IsPlayerDisorganized,
                PlayerSkipNextTurn = x.PlayerSkipNextTurn,
                PlayerPosition = x.PlayerPosition,
                LeftHandItemId = x.LeftHandItemId,
                RightHandItemId = x.RightHandItemId,
                StartedAt = x.StartedAt,
                FinishedAt = x.FinishedAt,
                Battles = x.Battles.Select(b => new AdminBattleDTO
                {
                    Id = b.Id,
                    Enemies = b.Enemies.Select(e => new AdminBattleEnemyDTO
                    {
                        Id = e.Id,
                        Class = e.Class,
                        Position = e.Position,
                        CurrentHp = e.CurrentHp,
                        MaxHp = e.MaxHp
                    }).ToList()
                }).ToList()
            })
            .FirstAsync();
    }

    public async Task BlockUserAsync(Guid adminId, Guid userId, string reason, int? days)
    {
        var user = await _context.Users.FirstAsync(x => x.Id == userId);

        user.IsBlocked = true;
        user.BlockReason = reason;
        user.BlockedUntil = days.HasValue
            ? DateTime.UtcNow.AddDays(days.Value)
            : null;

        await _context.SaveChangesAsync();

        await LogAsync(adminId, "BLOCK_USER", userId, new
        {
            reason,
            days
        });
        await (_realtimeNotifier?.AppChangedAsync("admin.user", "blocked", userId, new { reason, days }) ?? Task.CompletedTask);
    }

    public async Task UnblockUserAsync(Guid adminId, Guid userId)
    {
        var user = await _context.Users.FirstAsync(x => x.Id == userId);

        user.IsBlocked = false;
        user.BlockReason = null;
        user.BlockedUntil = null;

        await _context.SaveChangesAsync();

        await LogAsync(adminId, "UNBLOCK_USER", userId);
        await (_realtimeNotifier?.AppChangedAsync("admin.user", "unblocked", userId) ?? Task.CompletedTask);
    }

    public async Task ChangeRoleAsync(Guid adminId, Guid userId, UserRole role)
    {
        var user = await _context.Users.FirstAsync(x => x.Id == userId);

        var oldRole = user.Role;
        user.Role = role;

        await _context.SaveChangesAsync();

        await LogAsync(adminId, "CHANGE_ROLE", userId, new
        {
            oldRole,
            newRole = role
        });
        await (_realtimeNotifier?.AppChangedAsync("admin.user", "role-changed", userId, new { oldRole, role }) ?? Task.CompletedTask);
    }

    public async Task<MarketEconomyDTO> GetMarketplaceEconomyAsync()
    {
        var settings = await EnsureEconomySettingsAsync();
        return MapEconomy(settings);
    }

    public async Task<MarketEconomyDTO> UpdateMarketplaceEconomyAsync(Guid adminId, UpdateMarketplaceEconomyDTO dto)
    {
        ValidateEconomy(dto);
        await LogAsync(adminId, "UPDATE_MARKETPLACE_ECONOMY", adminId, dto);
        await (_realtimeNotifier?.AppChangedAsync("admin.market", "economy-updated", adminId) ?? Task.CompletedTask);
        return MapEconomy(ToSettings(dto));
    }

    public async Task<PagedResultDTO<AdminLogDTO>> GetAdminLogsAsync(AdminLogsQueryDTO query)
    {
        var q = _context.AdminLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(x => x.Action == query.Action);
        if (query.AdminId.HasValue)
            q = q.Where(x => x.AdminId == query.AdminId.Value);
        if (!string.IsNullOrWhiteSpace(query.TargetUserId))
            q = q.Where(x => x.TargetUserId == query.TargetUserId);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AdminLogDTO
            {
                Id = x.Id,
                AdminId = x.AdminId,
                Action = x.Action,
                TargetUserId = x.TargetUserId,
                CreatedAt = x.CreatedAt,
                Data = x.Data
            })
            .ToListAsync();

        return new PagedResultDTO<AdminLogDTO> { Items = items, TotalCount = total, Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<MarketplaceEconomyStatsDTO> GetMarketplaceEconomyStatsAsync()
    {
        var botBuyerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var transactions = await _context.Transactions.ToListAsync();
        var p2p = transactions.Where(x => x.BuyerId != botBuyerId).ToList();
        var bot = transactions.Where(x => x.BuyerId == botBuyerId).ToList();
        var activeListings = await _context.MarketListings.Where(x => !x.IsSold).ToListAsync();

        return new MarketplaceEconomyStatsDTO
        {
            TotalCurrencyHeldByPlayers = await _context.Users.SumAsync(x => x.Currency),
            TotalP2PVolume = p2p.Sum(x => (decimal)x.Price),
            TotalBotSaleVolume = bot.Sum(x => (decimal)x.Price),
            TotalTaxRemoved = transactions.Sum(x => x.TaxAmount),
            TotalP2PTaxRemoved = p2p.Sum(x => x.TaxAmount),
            TotalBotTaxRemoved = bot.Sum(x => x.TaxAmount),
            ActiveListings = activeListings.Count,
            ActiveListingsValue = activeListings.Sum(x => x.Price),
            TransactionCount = transactions.Count
        };
    }

    private async Task LogAsync(Guid adminId, string action, Guid targetUserId, object? data = null)
    {
        _context.AdminLogs.Add(new AdminLog
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            Action = action,
            TargetUserId = targetUserId.ToString(),
            Data = data == null ? null : System.Text.Json.JsonSerializer.Serialize(data)
        });

        await _context.SaveChangesAsync();
    }

    private async Task<EconomySettings> EnsureEconomySettingsAsync()
    {
        var serialized = await _context.AdminLogs
            .Where(x => x.Action == "UPDATE_MARKETPLACE_ECONOMY" && x.Data != null)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.Data)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(serialized))
        {
            var dto = System.Text.Json.JsonSerializer.Deserialize<UpdateMarketplaceEconomyDTO>(serialized);
            if (dto != null)
                return ToSettings(dto);
        }

        return new EconomySettings
        {
            DailyCurrencyReward = _economyOptions.DailyCurrencyReward,
            BotBasePrice = _economyOptions.BotBasePrice,
            BotStatMultiplier = _economyOptions.BotStatMultiplier,
            BotElementMultiplier = _economyOptions.BotElementMultiplier,
            IsPlayerToPlayerTaxEnabled = _economyOptions.IsPlayerToPlayerTaxEnabled,
            IsPlayerToBotTaxEnabled = _economyOptions.IsPlayerToBotTaxEnabled,
            ProgressiveTaxBrackets = _economyOptions.ProgressiveTaxBrackets.Select(x => new MarketTaxBracketDTO
            {
                From = x.From,
                To = x.To,
                Rate = x.Rate
            }).ToList()
        };
    }

    private static EconomySettings ToSettings(UpdateMarketplaceEconomyDTO dto)
    {
        return new EconomySettings
        {
            DailyCurrencyReward = dto.DailyCurrencyReward,
            BotBasePrice = dto.BotBasePrice,
            BotStatMultiplier = dto.BotStatMultiplier,
            BotElementMultiplier = dto.BotElementMultiplier,
            IsPlayerToPlayerTaxEnabled = dto.IsPlayerToPlayerTaxEnabled,
            IsPlayerToBotTaxEnabled = dto.IsPlayerToBotTaxEnabled,
            ProgressiveTaxBrackets = dto.ProgressiveTaxBrackets.OrderBy(x => x.From).ToList()
        };
    }

    private static MarketEconomyDTO MapEconomy(EconomySettings settings)
    {
        return new MarketEconomyDTO
        {
            DailyCurrencyReward = settings.DailyCurrencyReward,
            BotBasePrice = settings.BotBasePrice,
            BotStatMultiplier = settings.BotStatMultiplier,
            BotElementMultiplier = settings.BotElementMultiplier,
            IsPlayerToPlayerTaxEnabled = settings.IsPlayerToPlayerTaxEnabled,
            IsPlayerToBotTaxEnabled = settings.IsPlayerToBotTaxEnabled,
            BotSaleFormula = $"{settings.BotBasePrice} + (primary stats + element values * {settings.BotElementMultiplier}) * {settings.BotStatMultiplier}, rounded to full currency.",
            ProgressiveTaxBrackets = settings.ProgressiveTaxBrackets
                .OrderBy(x => x.From)
                .ToList()
        };
    }

    private sealed class EconomySettings
    {
        public decimal DailyCurrencyReward { get; set; }
        public decimal BotBasePrice { get; set; }
        public decimal BotStatMultiplier { get; set; }
        public decimal BotElementMultiplier { get; set; }
        public bool IsPlayerToPlayerTaxEnabled { get; set; }
        public bool IsPlayerToBotTaxEnabled { get; set; }
        public List<MarketTaxBracketDTO> ProgressiveTaxBrackets { get; set; } = new();
    }

    private static void ValidateEconomy(UpdateMarketplaceEconomyDTO dto)
    {
        if (dto.DailyCurrencyReward < 0 || dto.BotBasePrice < 0 || dto.BotStatMultiplier < 0 || dto.BotElementMultiplier < 0)
            throw new InvalidOperationException("Economy values cannot be negative.");
        if (dto.ProgressiveTaxBrackets.Count == 0)
            throw new InvalidOperationException("At least one tax bracket is required.");

        foreach (var bracket in dto.ProgressiveTaxBrackets)
        {
            if (bracket.From < 0 || bracket.Rate < 0 || bracket.Rate > 1)
                throw new InvalidOperationException("Invalid tax bracket.");
            if (bracket.To.HasValue && bracket.To.Value <= bracket.From)
                throw new InvalidOperationException("Tax bracket upper bound must be greater than lower bound.");
        }
    }

    private IQueryable<User> ApplyUserFilters(IQueryable<User> q, GetUsersQueryDTO query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(x =>
                x.Username.Contains(query.Search) ||
                x.Currency.ToString().Contains(query.Search));

        if (query.Role.HasValue)
            q = q.Where(x => x.Role == query.Role);

        if (query.IsBlocked.HasValue)
            q = q.Where(x => x.IsBlocked == query.IsBlocked);

        return q;
    }

    private IQueryable<User> ApplyUserSorting(IQueryable<User> q, string sortBy, string sortDir)
    {
        var desc = sortDir?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            AdminUserSortBy.Username =>
                desc ? q.OrderByDescending(x => x.Username)
                     : q.OrderBy(x => x.Username),

            AdminUserSortBy.Currency =>
                desc ? q.OrderByDescending(x => x.Currency)
                     : q.OrderBy(x => x.Currency),

            AdminUserSortBy.Role =>
                desc ? q.OrderByDescending(x => x.Role)
                     : q.OrderBy(x => x.Role),

            AdminUserSortBy.Blocked =>
                desc ? q.OrderByDescending(x => x.IsBlocked)
                     : q.OrderBy(x => x.IsBlocked),

            _ =>
                q.OrderBy(x => x.Username)
        };
    }

    private static Expression<Func<Run, AdminRunListDTO>> MapRunList()
        => x => new AdminRunListDTO
        {
            Id = x.Id,
            UserId = x.UserId,
            Status = x.Status,
            BattleIndex = x.BattleIndex,
            PlayerCurrentHp = x.PlayerCurrentHp,
            PlayerMaxHp = x.PlayerMaxHp,
            StartedAt = x.StartedAt,
            FinishedAt = x.FinishedAt
        };
}
