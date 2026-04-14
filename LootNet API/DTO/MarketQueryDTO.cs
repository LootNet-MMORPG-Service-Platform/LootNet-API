using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class MarketQueryDTO
{
    public string? Search { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string SortColumn { get; set; } = "Price";
    public string SortDirection { get; set; } = "asc";

    public List<ItemCategory>? Categories { get; set; }

    public List<WeaponType>? WeaponTypes { get; set; }
    public List<ArmorType>? ArmorTypes { get; set; }
    public List<ItemElementType>? Elements { get; set; }

    public RangeFilter<decimal>? Price { get; set; }

    public RangeFilter<double>? Cut { get; set; }
    public RangeFilter<double>? Blunt { get; set; }

    public RangeFilter<double>? CutResistance { get; set; }
    public RangeFilter<double>? BluntResistance { get; set; }

    public RangeFilter<double>? ElementValue { get; set; }
}
