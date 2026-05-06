using Microsoft.AspNetCore.Http;

namespace LootNet_API.Services.Interfaces;

public interface IProfileService
{
    Task<string> UploadProfilePictureAsync(Guid userId, IFormFile file);
}
