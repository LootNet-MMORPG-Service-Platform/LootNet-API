using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LootNet_API.Data;
using LootNet_API.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

namespace LootNet_API.Services;

public class TokenService : ITokenService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly Random _rand = new();

    private readonly int _jwtMinutes;
    private readonly int _refreshDays;

    public TokenService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;

        _jwtMinutes = _config.GetValue<int>("Jwt:AccessTokenMinutes");
        _refreshDays = _config.GetValue<int>("Jwt:RefreshTokenDays");
    }

    public string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(Guid userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refresh = new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshDays)
        };
        _context.RefreshTokens.Add(refresh);
        _context.SaveChanges();
        return refresh;
    }

    public async Task<RefreshToken?> GetValidRefreshTokenAsync(string token)
    {
        var refresh = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token && !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow);
        return refresh;
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken token)
    {
        token.IsRevoked = true;
        await _context.SaveChangesAsync();
    }
}
