namespace LootNet_API.DTO.Items;

public class EquipmentResponseDTO
{
    public ArmorDTO? Head { get; set; }
    public ArmorDTO? Body { get; set; }
    public ArmorDTO? Gloves { get; set; }
    public ArmorDTO? Legs { get; set; }
    public ArmorDTO? Boots { get; set; }

    public WeaponDTO? Weapon1 { get; set; }
    public WeaponDTO? Weapon2 { get; set; }
    public WeaponDTO? Weapon3 { get; set; }
    public WeaponDTO? Weapon4 { get; set; }
}
