using LootNet_API.DTO.Validation;

namespace LootNet_API.DTO;

public class SellItemToBotDTO
{
    [NotEmptyGuid]
    public Guid ItemId { get; set; }
}

