using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;
using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class WeaponQueryDTO
{
    [StringLength(80)]
    public string? Search { get; set; }

    [MaxCollectionCount(20)]
    public List<WeaponType>? Types { get; set; }

    [MaxCollectionCount(20)]
    public List<ItemElementType>? Elements { get; set; }

    public RangeFilter<decimal>? Price { get; set; }
    public RangeFilter<double>? Cut { get; set; }
    public RangeFilter<double>? Blunt { get; set; }

    [Range(1, 10_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(32)]
    public string SortColumn { get; set; } = WeaponSortColumns.Price;

    public SortDirection SortDirection { get; set; } = SortDirection.Asc;
}
