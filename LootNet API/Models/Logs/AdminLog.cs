namespace LootNet_API.Models.Logs;

public class AdminLog
{
    public Guid Id { get; set; }
    public Guid AdminId { get; set; }

    public string Action { get; set; } = string.Empty;
    public string TargetUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Data { get; set; }
}
