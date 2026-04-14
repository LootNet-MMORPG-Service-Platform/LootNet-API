using LootNet_API.Models;

namespace LootNet_API.Services.Interfaces;

public interface ITokenService
{
    string GenerateJwt(User user);
    RefreshToken GenerateRefreshToken(Guid userId);
    Task<RefreshToken?> GetValidRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(RefreshToken token);
}
