using System.Xml.Linq;
using LootNet_API.Enums;

namespace LootNet_API.Models.Items;

public class Armor : Item
{
    public ArmorType ArmorType { get; set; }
    public double CutResistance { get; set; }
    public double BluntResistance { get; set; }
    public List<ItemElement> Elements { get; set; } = new();
}
