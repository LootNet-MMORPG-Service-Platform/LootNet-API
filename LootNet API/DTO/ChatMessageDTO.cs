namespace LootNet_API.DTO;

public class ChatMessageDTO
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string? SenderProfileImagePath { get; set; }
    public Guid? RecipientId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
