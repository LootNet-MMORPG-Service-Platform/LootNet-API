using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;
using LootNet_API.Enums;

namespace LootNet_API.DTO.Market;

public class SellInventoryQueryDTO
{
    [Required]
    [AllowedStringValues("all", "weapon", "armor")]
    public string ItemType { get; set; } = "all";

    [StringLength(80)]
    public string? Search { get; set; }

    [Required]
    [AllowedStringValues("power", "price", "name")]
    public string SortBy { get; set; } = "power";

    public SortDirection SortDirection { get; set; } = SortDirection.Desc;

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal? PriceHint { get; set; }

    [Range(1, 10_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 30;
}

