using System.ComponentModel.DataAnnotations;

namespace LootNet_API.DTO.Admin;

public class BlockUserDTO
{
    [Range(1, 3650)]
    public int? Days { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}

