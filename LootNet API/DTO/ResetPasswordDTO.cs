namespace LootNet_API.DTO;

public class ResetPasswordDTO
{
    public required string OldPassword { get; set; }
    public required string NewPassword { get; set; }
}
