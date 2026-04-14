using LootNet_API.DTO.Items;
using LootNet_API.Enums;

namespace LootNet_API.DTO;

public class WeaponMarketDTO
{
    public Guid ListingId { get; set; }
    public Guid ItemId { get; set; }

    public string Name { get; set; } = "";

    public decimal Price { get; set; }

    public WeaponType WeaponType { get; set; }

    public double Cut { get; set; }
    public double Blunt { get; set; }

    public List<ItemElementDTO> Elements { get; set; } = new();
}
