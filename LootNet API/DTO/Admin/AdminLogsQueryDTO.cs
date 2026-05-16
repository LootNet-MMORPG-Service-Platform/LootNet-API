namespace LootNet_API.DTO.Admin;

public class AdminLogsQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public string? Action { get; set; }
    public Guid? AdminId { get; set; }
    public string? TargetUserId { get; set; }
}
