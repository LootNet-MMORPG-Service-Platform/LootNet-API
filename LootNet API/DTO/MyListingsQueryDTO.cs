using System.ComponentModel.DataAnnotations;
using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class MyListingsQueryDTO
{
    [StringLength(80)]
    public string? Search { get; set; }

    public ItemCategory? Category { get; set; }
    public RangeFilter<decimal>? Price { get; set; }

    [Range(1, 10_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

