namespace LootNet_API.DTO
{
    using System.ComponentModel.DataAnnotations;

    public class ForgotPasswordDTO
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public required string Email { get; set; }
    }
}
