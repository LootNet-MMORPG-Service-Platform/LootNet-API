namespace LootNet_API.DTO;

public class SendPrivateChatMessageDTO
{
    public Guid RecipientId { get; set; }
    public string Text { get; set; } = string.Empty;
}
