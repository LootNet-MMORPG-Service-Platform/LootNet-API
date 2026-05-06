using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class MyListingsQueryDTO
{
    public string? Search { get; set; }
    public ItemCategory? Category { get; set; }
    public RangeFilter<decimal>? Price { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
