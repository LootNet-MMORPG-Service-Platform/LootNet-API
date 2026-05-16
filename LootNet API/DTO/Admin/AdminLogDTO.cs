namespace LootNet_API.DTO.Admin;

public class AdminLogDTO
{
    public Guid Id { get; set; }
    public Guid AdminId { get; set; }
    public string Action { get; set; } = "";
    public string TargetUserId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? Data { get; set; }
}
