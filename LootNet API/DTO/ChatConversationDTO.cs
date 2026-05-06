namespace LootNet_API.DTO;

public class ChatConversationDTO
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? ProfileImagePath { get; set; }
    public string LastMessageText { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
}
