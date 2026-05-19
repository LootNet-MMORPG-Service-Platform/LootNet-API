using System.ComponentModel.DataAnnotations;
using LootNet_API.Enums;

namespace LootNet_API.DTO.Admin;

public class GetRunsQueryDTO
{
    public Guid? UserId { get; set; }
    public RunStatus? Status { get; set; }

    [Range(1, 10_000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

