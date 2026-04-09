using System.Xml.Linq;
using LootNet_API.Enums;

namespace LootNet_API.Models.Items;

public class Weapon : Item
{
    public WeaponType WeaponType { get; set; }
    public double Cut { get; set; }
    public double Blunt { get; set; }
    public List<ItemElement> Elements { get; set; } = new();
}
