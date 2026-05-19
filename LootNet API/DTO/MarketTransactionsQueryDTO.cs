using System.ComponentModel.DataAnnotations;

namespace LootNet_API.DTO;

public class MarketTransactionsQueryDTO : IValidatableObject
{
    [StringLength(80)]
    public string? Search { get; set; }

    public bool? IsSale { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public RangeFilter<int>? Price { get; set; }

    [Range(1, 10_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From.Value > To.Value)
            yield return new ValidationResult("From date cannot be later than To date.");
    }
}
