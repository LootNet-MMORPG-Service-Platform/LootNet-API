namespace LootNet_API.DTO.Items;

public class ItemCollectionDTO
{
    public List<WeaponDTO> Weapons { get; set; } = [];
    public List<ArmorDTO> Armors { get; set; } = [];
}
