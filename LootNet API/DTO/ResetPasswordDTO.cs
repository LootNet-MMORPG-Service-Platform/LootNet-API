namespace LootNet_API.DTO;

using System.ComponentModel.DataAnnotations;

public class ResetPasswordDTO
{
    [Required]
    [StringLength(128)]
    public required string OldPassword { get; set; }

    [Required]
    [StringLength(128)]
    public required string NewPassword { get; set; }
}
