namespace LootNet_API.DTO
{
    using System.ComponentModel.DataAnnotations;

    public class LoginDTO
    {
        [Required]
        [StringLength(32, MinimumLength = 3)]
        public required string Username { get; set; }

        [Required]
        [StringLength(128)]
        public required string Password { get; set; }
    }
}
