namespace LootNet_API.DTO
{
    using System.ComponentModel.DataAnnotations;

    public class LoginDTO : IValidatableObject
    {
        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; set; }

        [StringLength(256, MinimumLength = 3)]
        public string? Username { get; set; }

        [Required]
        [StringLength(128)]
        public required string Password { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(Username))
                yield return new ValidationResult("Email is required.", new[] { nameof(Email) });
        }
    }
}
