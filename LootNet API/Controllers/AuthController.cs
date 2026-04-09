namespace LootNet_API.Controllers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Data;
using DTO;
using Enums;
using LootNet_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Models;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ITokenService _tokenService;

    public AuthController(AppDbContext context, IConfiguration config, ITokenService tokenService)
    {
        _context = context;
        _config = config;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
    {
        if (_context.Users.Any(u => u.Username == dto.Username))
            return BadRequest("User already exists");

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            PasswordHash = hash,
            Role = UserRole.Player,
            Currency = 1000
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDTO dto)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == dto.Username);

        if (user == null)
            return Unauthorized();

        var valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!valid)
            return Unauthorized();

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

        return Ok("Password changed");
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);

        if (user == null)
            return NotFound();

        return Ok(new UserProfileDTO
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role,
            Currency = user.Currency
        });
    }
}

