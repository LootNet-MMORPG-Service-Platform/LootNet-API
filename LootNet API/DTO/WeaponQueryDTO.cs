using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class WeaponQueryDTO
{
    public string? Search { get; set; }

    public List<WeaponType>? Types { get; set; }
    public List<ItemElementType>? Elements { get; set; }

    public RangeFilter<decimal>? Price { get; set; }
    public RangeFilter<double>? Cut { get; set; }
    public RangeFilter<double>? Blunt { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string SortColumn { get; set; } = WeaponSortColumns.Price;
    public SortDirection SortDirection { get; set; } = SortDirection.Asc;
}
