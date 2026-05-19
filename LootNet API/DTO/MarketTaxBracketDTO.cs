using System.ComponentModel.DataAnnotations;

namespace LootNet_API.DTO;

public class MarketTaxBracketDTO
{
    [Range(typeof(decimal), "0", "1000000000")]
    public decimal From { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal? To { get; set; }

    [Range(typeof(decimal), "0", "1")]
    public decimal Rate { get; set; }
}

