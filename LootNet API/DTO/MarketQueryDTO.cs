using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;
using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class MarketQueryDTO
{
    [StringLength(80)]
    public string? Search { get; set; }

    [Range(1, 10_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [Required]
    [StringLength(32)]
    public string SortColumn { get; set; } = "Price";

    [Required]
    [AllowedStringValues("asc", "desc")]
    public string SortDirection { get; set; } = "asc";

    [MaxCollectionCount(20)]
    public List<ItemCategory>? Categories { get; set; }

    [MaxCollectionCount(20)]
    public List<WeaponType>? WeaponTypes { get; set; }

    [MaxCollectionCount(20)]
    public List<ArmorType>? ArmorTypes { get; set; }

    [MaxCollectionCount(20)]
    public List<ItemElementType>? Elements { get; set; }

    public RangeFilter<decimal>? Price { get; set; }
    public RangeFilter<double>? Cut { get; set; }
    public RangeFilter<double>? Blunt { get; set; }
    public RangeFilter<double>? CutResistance { get; set; }
    public RangeFilter<double>? BluntResistance { get; set; }
    public RangeFilter<double>? ElementValue { get; set; }
}
