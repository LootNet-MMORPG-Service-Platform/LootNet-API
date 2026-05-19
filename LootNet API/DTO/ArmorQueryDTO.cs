using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;
using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class ArmorQueryDTO
{
    [StringLength(80)]
    public string? Search { get; set; }

    [MaxCollectionCount(20)]
    public List<ArmorType>? Types { get; set; }

    [MaxCollectionCount(20)]
    public List<ItemElementType>? Elements { get; set; }

    public RangeFilter<decimal>? Price { get; set; }
    public RangeFilter<double>? CutResistance { get; set; }
    public RangeFilter<double>? BluntResistance { get; set; }

    [Range(1, 10_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(32)]
    public string SortColumn { get; set; } = ArmorSortColumns.Price;

    public SortDirection SortDirection { get; set; } = SortDirection.Asc;
}
