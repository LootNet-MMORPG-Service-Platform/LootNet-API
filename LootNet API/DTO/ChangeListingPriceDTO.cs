using System.ComponentModel.DataAnnotations;

namespace LootNet_API.DTO;

public class ChangeListingPriceDTO
{
    [Range(typeof(decimal), "0.01", "1000000000")]
    public decimal Price { get; set; }
}

