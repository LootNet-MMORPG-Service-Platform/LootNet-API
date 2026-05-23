using LootNet_API.DTO;

namespace LootNet_API.Services.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterDTO dto);
    Task<AuthResponseDTO> LoginAsync(LoginDTO dto);
    Task<AuthResponseDTO> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task ResetPasswordAsync(Guid userId, ResetPasswordDTO dto);
    Task RequestPasswordResetAsync(string email);
    Task ResetPasswordByEmailAsync(ResetPasswordByEmailDTO dto);
    Task VerifyEmailAsync(string token);
    Task ResendEmailVerificationAsync(string email);
    Task<int> DeleteExpiredUnverifiedUsersAsync();
}
