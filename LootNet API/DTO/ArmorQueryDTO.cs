using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class ArmorQueryDTO
{
    public string? Search { get; set; }

    public List<ArmorType>? Types { get; set; }
    public List<ItemElementType>? Elements { get; set; }

    public RangeFilter<decimal>? Price { get; set; }
    public RangeFilter<double>? CutResistance { get; set; }
    public RangeFilter<double>? BluntResistance { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string SortColumn { get; set; } = ArmorSortColumns.Price;
    public SortDirection SortDirection { get; set; } = SortDirection.Asc;
}
