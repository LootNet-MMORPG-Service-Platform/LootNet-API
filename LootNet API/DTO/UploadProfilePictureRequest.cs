using System.ComponentModel.DataAnnotations;

namespace LootNet_API.DTO;

public class UploadProfilePictureRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
}

