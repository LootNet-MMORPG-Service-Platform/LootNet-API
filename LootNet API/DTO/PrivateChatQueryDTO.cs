namespace LootNet_API.DTO;

public class PrivateChatQueryDTO
{
    public Guid OtherUserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
