using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;

namespace LootNet_API.DTO;

public class CreateMarketListingDTO
{
    [NotEmptyGuid]
    public Guid ItemId { get; set; }

    [Range(typeof(decimal), "0.01", "1000000000")]
    public decimal Price { get; set; }
}

