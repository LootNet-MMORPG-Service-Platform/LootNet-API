namespace LootNet_API.DTO
{
    using System.ComponentModel.DataAnnotations;

    public class ResetPasswordByEmailDTO
    {
        [Required]
        public required string Token { get; set; }

        [Required]
        [StringLength(128)]
        public required string NewPassword { get; set; }
    }
}
