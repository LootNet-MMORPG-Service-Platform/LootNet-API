namespace LootNet_API.Tests;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using LootNet_API.Controllers;
using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Services;
using LootNet_API.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Xunit;

public class AuthControllerTests
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly TokenService _tokenService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _db = TestDbContextFactory.Create();

        var inMemorySettings = new Dictionary<string, string>
        {
            {"Jwt:Key", "SuperSecretKey12345678901234567890"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"},
            {"Jwt:AccessTokenMinutes", "60"},
            {"Jwt:RefreshTokenDays", "7"}
        };

        _config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        _tokenService = new TokenService(_db, _config);
        _controller = new AuthController(_db, _config, _tokenService);
    }

    [Fact]
    public async Task Register_CreatesUser_WhenNew()
    {
        var dto = new RegisterDTO { Username = "player1", Password = "password" };

        var result = await _controller.Register(dto);

        Assert.IsType<OkResult>(result);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == "player1");
        Assert.NotNull(user);
    }

    [Fact]
    public async Task Register_Fails_WhenUserExists()
    {
        _db.Users.Add(new User { Id = Guid.NewGuid(), Username = "player1", PasswordHash = "hash", Equipment = new Equipment() });
        await _db.SaveChangesAsync();

        var dto = new RegisterDTO { Username = "player1", Password = "password" };
        var result = await _controller.Register(dto);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("User already exists", bad.Value);
    }

    [Fact]
    public async Task Login_ReturnsTokens_WhenValid()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("password");
        var user = new User { Id = Guid.NewGuid(), Username = "player1", PasswordHash = hash, Role = UserRole.Player, Equipment = new Equipment() };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var dto = new LoginDTO { Username = "player1", Password = "password" };
        var result = _controller.Login(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        var obj = Assert.IsType<AuthResponseDTO>(ok.Value);
        Assert.NotNull(obj.Token);
        Assert.NotNull(obj.RefreshToken);
    }

    [Fact]
    public void Login_Fails_WhenWrongPassword()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("password");
        _db.Users.Add(new User { Id = Guid.NewGuid(), Username = "player1", PasswordHash = hash, Role = UserRole.Player, Equipment = new Equipment() });
        _db.SaveChanges();

        var dto = new LoginDTO { Username = "player1", Password = "wrong" };
        var result = _controller.Login(dto);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Refresh_ReturnsNewTokens_WhenValid()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "player1", PasswordHash = "hash", Role = UserRole.Player, Equipment = new Equipment() };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var refresh = _tokenService.GenerateRefreshToken(user.Id);
        await _db.SaveChangesAsync();

        var result = await _controller.Refresh(refresh.Token);
        var ok = Assert.IsType<OkObjectResult>(result);
        var obj = Assert.IsType<AuthResponseDTO>(ok.Value);
        Assert.NotNull(obj.Token);
        Assert.NotNull(obj.RefreshToken);
        Assert.NotEqual(refresh.Token, obj.RefreshToken);
    }

    [Fact]
    public async Task Refresh_Fails_WhenInvalidToken()
    {
        var result = await _controller.Refresh("invalid");
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid or expired refresh token", unauthorized.Value);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "player1", PasswordHash = "hash", Role = UserRole.Player, Equipment = new Equipment() };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var refresh = _tokenService.GenerateRefreshToken(user.Id);
        await _db.SaveChangesAsync();

        var result = await _controller.Logout(refresh.Token);
        Assert.IsType<OkResult>(result);

        var dbToken = await _db.RefreshTokens.FindAsync(refresh.Id);
        Assert.True(dbToken.IsRevoked);
    }

    [Fact]
    public async Task ResetPassword_Succeeds_WhenOldPasswordCorrect()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "player1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass"), Equipment = new Equipment() };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var dto = new ResetPasswordDTO { OldPassword = "oldpass", NewPassword = "newpass" };

        var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claims } };

        var result = await _controller.ResetPassword(dto);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Password changed", ok.Value);

        var updated = await _db.Users.FindAsync(user.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("newpass", updated.PasswordHash));
    }

    [Fact]
    public async Task ResetPassword_Fails_WhenOldPasswordWrong()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "player1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass"), Equipment = new Equipment() };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var dto = new ResetPasswordDTO { OldPassword = "wrong", NewPassword = "newpass" };

        var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claims } };

        var result = await _controller.ResetPassword(dto);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Wrong password", bad.Value);
    }
}