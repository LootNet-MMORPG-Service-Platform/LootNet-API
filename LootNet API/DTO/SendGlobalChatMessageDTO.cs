using System.ComponentModel.DataAnnotations;

namespace LootNet_API.DTO;

public class SendGlobalChatMessageDTO
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;
}

