namespace LootNet_API.Controllers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Data;
using DTO;
using Enums;
using LootNet_API.Models.Items.Generation;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Models;

[Route("api/auth")]
[ApiController]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ITokenService _tokenService;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public AuthController(AppDbContext context, IConfiguration config, ITokenService tokenService, IRealtimeNotifier realtimeNotifier)
    {
        _context = context;
        _config = config;
        _tokenService = tokenService;
        _realtimeNotifier = realtimeNotifier;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
    {
        if (_context.Users.Any(u => u.Username == dto.Username))
            return BadRequest("User already exists");

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var profileId = await EnsureDefaultProfileAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            PasswordHash = hash,
            Role = UserRole.Player,
            ProfileId = profileId,
            Equipment = new Equipment()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        await _realtimeNotifier.AppChangedAsync("auth", "user-registered", user.Id);

        return Ok();
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDTO dto)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == dto.Username);

        if (user == null)
            return Unauthorized("Invalid username or password.");

        if (user.IsBlocked)
        {
            var blockedUntil = user.BlockedUntil?.ToString("u") ?? "indefinitely";
            var reason = string.IsNullOrWhiteSpace(user.BlockReason) ? "No reason provided." : user.BlockReason;
            return StatusCode(StatusCodes.Status403Forbidden, $"Account is blocked until {blockedUntil}. Reason: {reason}");
        }

        var valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!valid)
            return Unauthorized("Invalid username or password.");

        var token = _tokenService.GenerateJwt(user);
        var refresh = _tokenService.GenerateRefreshToken(user.Id);

        return Ok(new AuthResponseDTO
        {
            Token = token,
            RefreshToken = refresh.Token
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var token = await _tokenService.GetValidRefreshTokenAsync(refreshToken);
        if (token == null) return Unauthorized("Invalid or expired refresh token");

        var user = _context.Users.FirstOrDefault(u => u.Id == token.UserId);
        if (user == null) return Unauthorized();

        var newJwt = _tokenService.GenerateJwt(user);
        await _tokenService.RevokeRefreshTokenAsync(token);
        var newRefresh = _tokenService.GenerateRefreshToken(user.Id);

        return Ok(new AuthResponseDTO
        {
            Token = newJwt,
            RefreshToken = newRefresh.Token
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        var token = await _tokenService.GetValidRefreshTokenAsync(refreshToken);
        if (token != null)
            await _tokenService.RevokeRefreshTokenAsync(token);

        return Ok();
    }

    [HttpPost("reset-password")]
    [Authorize]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);
        if (user == null) return NotFound();

        var valid = BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash);
        if (!valid)
            return BadRequest("Wrong password");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        await _context.SaveChangesAsync();
        await _realtimeNotifier.AppChangedAsync("auth", "password-reset", user.Id);

        return Ok("Password changed");
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
}

