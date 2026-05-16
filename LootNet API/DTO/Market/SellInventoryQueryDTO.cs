using LootNet_API.Enums;

namespace LootNet_API.DTO.Market;

public class SellInventoryQueryDTO
{
    public string ItemType { get; set; } = "all"; // all|weapon|armor
    public string? Search { get; set; }
    public string SortBy { get; set; } = "power"; // power|price|name
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
    public decimal? PriceHint { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
