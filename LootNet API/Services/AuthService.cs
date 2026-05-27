using System.Security.Cryptography;
using System.Text;
using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items.Generation;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LootNet_API.Services;

public class AuthService : IAuthService
{
    private static readonly TimeSpan EmailVerificationTokenLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ITokenService _tokenService;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IEmailSender _emailSender;

    public AuthService(
        AppDbContext context,
        IConfiguration config,
        ITokenService tokenService,
        IRealtimeNotifier realtimeNotifier,
        IEmailSender emailSender)
    {
        _context = context;
        _config = config;
        _tokenService = tokenService;
        _realtimeNotifier = realtimeNotifier;
        _emailSender = emailSender;
    }

    public async Task RegisterAsync(RegisterDTO dto)
    {
        await DeleteExpiredUnverifiedUsersAsync();

        var username = dto.Username.Trim();
        var email = NormalizeEmail(dto.Email);
        ValidatePassword(dto.Password);

        if (await _context.Users.AnyAsync(u => u.Username == username))
            throw new InvalidOperationException("User already exists");

        if (await _context.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Email already exists");

        var profileId = await EnsureDefaultProfileAsync();
        var verificationToken = GenerateToken();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            EmailVerified = false,
            EmailVerificationTokenHash = HashToken(verificationToken),
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.Add(EmailVerificationTokenLifetime),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Player,
            ProfileId = profileId,
            Equipment = new Equipment()
        };

        await _emailSender.SendEmailVerificationAsync(
            user.Email,
            user.Username,
            BuildVerificationUrl(verificationToken, dto.VerificationClient));

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        await _realtimeNotifier.AppChangedAsync("auth", "user-registered", user.Id);
    }

    public async Task<AuthResponseDTO> LoginAsync(LoginDTO dto)
    {
        await DeleteExpiredUnverifiedUsersAsync();

        var identifier = !string.IsNullOrWhiteSpace(dto.Email) ? dto.Email : dto.Username;
        var normalizedEmail = NormalizeEmail(identifier ?? string.Empty);
        var normalizedUsername = identifier?.Trim();

        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Email == normalizedEmail || u.Username == normalizedUsername);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        if (user.IsBlocked)
        {
            var blockedUntil = user.BlockedUntil?.ToString("u") ?? "indefinitely";
            var reason = string.IsNullOrWhiteSpace(user.BlockReason) ? "No reason provided." : user.BlockReason;
            throw new AuthForbiddenException($"Account is blocked until {blockedUntil}. Reason: {reason}");
        }

        if (!user.EmailVerified)
            throw new AuthForbiddenException("Email is not verified.");

        var token = _tokenService.GenerateJwt(user);
        var refresh = _tokenService.GenerateRefreshToken(user.Id);

        return new AuthResponseDTO
        {
            Token = token,
            RefreshToken = refresh.Token
        };
    }

    public async Task<AuthResponseDTO> RefreshAsync(string refreshToken)
    {
        var token = await _tokenService.GetValidRefreshTokenAsync(refreshToken);
        if (token == null) throw new UnauthorizedAccessException("Invalid or expired refresh token");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == token.UserId);
        if (user == null) throw new UnauthorizedAccessException();
        if (user.IsBlocked)
        {
            await _tokenService.RevokeRefreshTokenAsync(token);
            var blockedUntil = user.BlockedUntil?.ToString("u") ?? "indefinitely";
            var reason = string.IsNullOrWhiteSpace(user.BlockReason) ? "No reason provided." : user.BlockReason;
            throw new AuthForbiddenException($"Account is blocked until {blockedUntil}. Reason: {reason}");
        }

        var newJwt = _tokenService.GenerateJwt(user);
        await _tokenService.RevokeRefreshTokenAsync(token);
        var newRefresh = _tokenService.GenerateRefreshToken(user.Id);

        return new AuthResponseDTO
        {
            Token = newJwt,
            RefreshToken = newRefresh.Token
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var token = await _tokenService.GetValidRefreshTokenAsync(refreshToken);
        if (token != null)
            await _tokenService.RevokeRefreshTokenAsync(token);
    }

    public async Task ResetPasswordAsync(Guid userId, ResetPasswordDTO dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) throw new KeyNotFoundException();

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            throw new InvalidOperationException("Wrong password");

        ValidatePassword(dto.NewPassword);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        await _context.SaveChangesAsync();
        await _realtimeNotifier.AppChangedAsync("auth", "password-reset", user.Id);
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
            return;

        var resetToken = GenerateToken();
        user.PasswordResetTokenHash = HashToken(resetToken);
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.Add(PasswordResetTokenLifetime);

        await _context.SaveChangesAsync();
        await _emailSender.SendPasswordResetAsync(user.Email, user.Username, BuildPasswordResetUrl(resetToken));
    }

    public async Task ResetPasswordByEmailAsync(ResetPasswordByEmailDTO dto)
    {
        var tokenHash = HashToken(dto.Token);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetTokenHash == tokenHash);

        if (user == null)
            throw new InvalidOperationException("Invalid password reset token.");

        if (user.PasswordResetTokenExpiresAt is null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Password reset token expired.");

        ValidatePassword(dto.NewPassword);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;

        await _context.SaveChangesAsync();
        await _realtimeNotifier.AppChangedAsync("auth", "password-reset", user.Id);
    }

    public async Task VerifyEmailAsync(string token)
    {
        var tokenHash = HashToken(token);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationTokenHash == tokenHash);

        if (user == null)
            throw new InvalidOperationException("Invalid email verification token.");

        if (user.EmailVerificationTokenExpiresAt is null || user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
        {
            if (!user.EmailVerified)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            throw new InvalidOperationException("Email verification token expired.");
        }

        user.EmailVerified = true;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAt = null;

        await _context.SaveChangesAsync();
        await _realtimeNotifier.AppChangedAsync("auth", "email-verified", user.Id);
    }

    public async Task ResendEmailVerificationAsync(string email)
    {
        await DeleteExpiredUnverifiedUsersAsync();

        var normalizedEmail = NormalizeEmail(email);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
            return;

        if (user.EmailVerified)
            return;

        var verificationToken = GenerateToken();
        user.EmailVerificationTokenHash = HashToken(verificationToken);
        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.Add(EmailVerificationTokenLifetime);

        await _context.SaveChangesAsync();
        await _emailSender.SendEmailVerificationAsync(user.Email, user.Username, BuildVerificationUrl(verificationToken, EmailVerificationClient.Web));
    }

    public async Task<int> DeleteExpiredUnverifiedUsersAsync()
    {
        var now = DateTime.UtcNow;
        var expiredUsers = await _context.Users
            .Where(u => !u.EmailVerified
                && u.EmailVerificationTokenExpiresAt != null
                && u.EmailVerificationTokenExpiresAt < now)
            .ToListAsync();

        if (expiredUsers.Count == 0)
            return 0;

        _context.Users.RemoveRange(expiredUsers);
        await _context.SaveChangesAsync();
        return expiredUsers.Count;
    }

    private async Task<Guid> EnsureDefaultProfileAsync()
    {
        var profileId = await _context.GenerationProfiles
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        if (profileId != Guid.Empty)
            return profileId;

        var profile = new GenerationProfile
        {
            Id = Guid.NewGuid(),
            Name = "Default"
        };

        _context.GenerationProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return profile.Id;
    }

    private string BuildVerificationUrl(string token, EmailVerificationClient client)
    {
        if (client == EmailVerificationClient.Mobile)
        {
            var mobileBaseUrl = _config["App:MobileVerificationBaseUrl"]?.TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(mobileBaseUrl))
                return QueryHelpers.AddQueryString(mobileBaseUrl, "token", token);

            var apiBaseUrl = _config["App:PublicBaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
                throw new InvalidOperationException("Mobile email verification URL is not configured. Set App__MobileVerificationBaseUrl or App__PublicBaseUrl.");

            return QueryHelpers.AddQueryString($"{apiBaseUrl}/api/auth/verify-email", "token", token);
        }

        var webBaseUrl = GetWebBaseUrl();
        if (!string.IsNullOrWhiteSpace(webBaseUrl))
            return QueryHelpers.AddQueryString($"{webBaseUrl}/verify-email", "token", token);

        var fallbackApiBaseUrl = _config["App:PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(fallbackApiBaseUrl))
            throw new InvalidOperationException("Email verification base URL is not configured. Set App__PublicBaseUrl.");

        return QueryHelpers.AddQueryString($"{fallbackApiBaseUrl}/api/auth/verify-email", "token", token);
    }

    private string BuildPasswordResetUrl(string token)
    {
        var webBaseUrl = GetWebBaseUrl();
        if (string.IsNullOrWhiteSpace(webBaseUrl))
            throw new InvalidOperationException("Password reset web URL is not configured. Set App__WebBaseUrl.");

        return QueryHelpers.AddQueryString($"{webBaseUrl}/reset-password", "token", token);
    }

    private string? GetWebBaseUrl()
    {
        return _config["App:WebBaseUrl"]?.TrimEnd('/');
    }

    private void ValidatePassword(string password)
    {
        if (IsDevelopmentMode())
            return;

        if (password.Length < 12)
            throw new InvalidOperationException("Password must be at least 12 characters.");

        if (!password.Any(char.IsUpper))
            throw new InvalidOperationException("Password must contain at least one uppercase letter.");

        if (!password.Any(char.IsLower))
            throw new InvalidOperationException("Password must contain at least one lowercase letter.");

        if (!password.Any(char.IsDigit))
            throw new InvalidOperationException("Password must contain at least one digit.");

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new InvalidOperationException("Password must contain at least one special character.");
    }

    private bool IsDevelopmentMode()
    {
        return _config.GetValue<bool>("Development");
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
