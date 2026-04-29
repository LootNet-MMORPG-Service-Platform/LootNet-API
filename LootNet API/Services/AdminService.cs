using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.DTO.Admin;
using LootNet_API.DTO.Items;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Logs;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LootNet_API.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly IInventoryService _inventory;
    private readonly IEquipmentService _equipment;

    public AdminService(AppDbContext context, IInventoryService inventory, IEquipmentService equipment)
    {
        _context = context;
        _inventory = inventory;
        _equipment = equipment;
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
    }

    public async Task UnblockUserAsync(Guid adminId, Guid userId)
    {
        var user = await _context.Users.FirstAsync(x => x.Id == userId);

        user.IsBlocked = false;
        user.BlockReason = null;
        user.BlockedUntil = null;

        await _context.SaveChangesAsync();

        await LogAsync(adminId, "UNBLOCK_USER", userId);
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
}
