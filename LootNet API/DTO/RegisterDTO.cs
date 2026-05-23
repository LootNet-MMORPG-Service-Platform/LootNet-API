namespace LootNet_API.DTO
{
    using System.ComponentModel.DataAnnotations;

    public class RegisterDTO
    {
        [Required]
        [StringLength(32, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Username can contain only letters, numbers, dots, dashes and underscores.")]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public required string Email { get; set; }

        [Required]
        [MinLength(8)]
        [StringLength(128)]
        public required string Password { get; set; }
    }
}
