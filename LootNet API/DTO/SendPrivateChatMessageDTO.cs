using System.ComponentModel.DataAnnotations;
using LootNet_API.DTO.Validation;

namespace LootNet_API.DTO;

public class SendPrivateChatMessageDTO
{
    [NotEmptyGuid]
    public Guid RecipientId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;
}

