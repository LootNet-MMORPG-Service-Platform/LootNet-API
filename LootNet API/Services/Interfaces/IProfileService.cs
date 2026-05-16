using Microsoft.AspNetCore.Http;
using LootNet_API.DTO;

namespace LootNet_API.Services.Interfaces;

public interface IProfileService
{
    Task<UserProfileDTO> GetProfileAsync(Guid userId);
    Task<string> UploadProfilePictureAsync(Guid userId, IFormFile file);
}
