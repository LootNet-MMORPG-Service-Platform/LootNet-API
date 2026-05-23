namespace LootNet_API.DTO
{
    using System.ComponentModel.DataAnnotations;

    public class VerifyEmailDTO
    {
        [Required]
        public required string Token { get; set; }
    }
}
