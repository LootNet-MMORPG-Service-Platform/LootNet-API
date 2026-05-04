namespace LootNet_API.DTO.Admin;

public class AdminBattleDTO
{
    public Guid Id { get; set; }
    public List<AdminBattleEnemyDTO> Enemies { get; set; } = new();
}
