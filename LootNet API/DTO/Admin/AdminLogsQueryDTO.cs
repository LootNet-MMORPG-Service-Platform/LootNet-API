using System.ComponentModel.DataAnnotations;

namespace LootNet_API.DTO.Admin;

public class AdminLogsQueryDTO
{
    [Range(1, 10_000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 30;

    [StringLength(80)]
    public string? Action { get; set; }

    public Guid? AdminId { get; set; }

    [StringLength(64)]
    public string? TargetUserId { get; set; }
}

