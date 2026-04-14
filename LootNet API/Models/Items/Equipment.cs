using LootNet_API.Models.Items;

public class Equipment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? HeadId { get; set; }
    public Guid? BodyId { get; set; }
    public Guid? GlovesId { get; set; }
    public Guid? LegsId { get; set; }
    public Guid? BootsId { get; set; }

    public Guid? WeaponSlot1Id { get; set; }
    public Guid? WeaponSlot2Id { get; set; }
    public Guid? WeaponSlot3Id { get; set; }
    public Guid? WeaponSlot4Id { get; set; }
}