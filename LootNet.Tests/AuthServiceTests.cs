namespace LootNet_API.Tests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.Models;
using LootNet_API.Services;
using LootNet_API.Services.Interfaces;
using LootNet_API.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

public class AuthServiceTests
{
    private readonly AppDbContext _db;
    private readonly AuthService _service;
    private readonly FakeEmailSender _emailSender;

    public AuthServiceTests()
    {
        _db = TestDbContextFactory.Create();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            {"Jwt:Key", "SuperSecretKey12345678901234567890"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"},
            {"Jwt:AccessTokenMinutes", "60"},
            {"Jwt:RefreshTokenDays", "7"},
            {"App:PublicBaseUrl", "https://lootnet-api.test"},
            {"App:WebBaseUrl", "https://lootnet-web.test"}
        }).Build();

        _emailSender = new FakeEmailSender();
        _service = new AuthService(_db, config, new TokenService(_db, config), new FakeRealtimeNotifier(), _emailSender);
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmail_AndSendsVerificationEmail()
    {
        await _service.RegisterAsync(new RegisterDTO { Username = "player1", Email = "PLAYER1@Example.COM ", Password = "password" });

        var user = await _db.Users.SingleAsync();
        Assert.Equal("player1@example.com", user.Email);
        Assert.False(user.EmailVerified);
        Assert.NotNull(user.EmailVerificationTokenHash);
        Assert.NotNull(user.EmailVerificationTokenExpiresAt);
        Assert.Equal("player1@example.com", _emailSender.LastEmail);
        Assert.Contains("/verify-email?token=", _emailSender.LastVerificationUrl);
    }

    [Fact]
    public async Task LoginAsync_RejectsUnverifiedEmail()
    {
        _db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "player1",
            Email = "player1@example.com",
            EmailVerified = false,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Equipment = new Equipment()
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AuthForbiddenException>(() =>
            _service.LoginAsync(new LoginDTO { Username = "player1", Password = "password" }));

        Assert.Equal("Email is not verified.", ex.Message);
    }

    [Fact]
    public async Task VerifyEmailAsync_MarksUserAsVerified()
    {
        await _service.RegisterAsync(new RegisterDTO { Username = "player1", Email = "player1@example.com", Password = "password" });
        var token = ExtractToken(_emailSender.LastVerificationUrl);

        await _service.VerifyEmailAsync(token);

        var user = await _db.Users.SingleAsync();
        Assert.True(user.EmailVerified);
        Assert.Null(user.EmailVerificationTokenHash);
        Assert.Null(user.EmailVerificationTokenExpiresAt);
    }

    [Fact]
    public async Task ResendEmailVerificationAsync_RotatesToken()
    {
        await _service.RegisterAsync(new RegisterDTO { Username = "player1", Email = "player1@example.com", Password = "password" });
        var user = await _db.Users.SingleAsync();
        var firstHash = user.EmailVerificationTokenHash;

        await _service.ResendEmailVerificationAsync("player1@example.com");

        Assert.NotEqual(firstHash, user.EmailVerificationTokenHash);
        Assert.Contains("/verify-email?token=", _emailSender.LastVerificationUrl);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_SendsResetEmail()
    {
        _db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "player1",
            Email = "player1@example.com",
            EmailVerified = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Equipment = new Equipment()
        });
        await _db.SaveChangesAsync();

        await _service.RequestPasswordResetAsync("PLAYER1@example.com");

        var user = await _db.Users.SingleAsync();
        Assert.NotNull(user.PasswordResetTokenHash);
        Assert.NotNull(user.PasswordResetTokenExpiresAt);
        Assert.Equal("player1@example.com", _emailSender.LastPasswordResetEmail);
        Assert.Contains("/reset-password?token=", _emailSender.LastPasswordResetUrl);
    }

    [Fact]
    public async Task ResetPasswordByEmailAsync_ChangesPassword_AndClearsToken()
    {
        _db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "player1",
            Email = "player1@example.com",
            EmailVerified = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword"),
            Equipment = new Equipment()
        });
        await _db.SaveChangesAsync();
        await _service.RequestPasswordResetAsync("player1@example.com");
        var token = ExtractToken(_emailSender.LastPasswordResetUrl);

        await _service.ResetPasswordByEmailAsync(new ResetPasswordByEmailDTO
        {
            Token = token,
            NewPassword = "newpassword"
        });

        var user = await _db.Users.SingleAsync();
        Assert.True(BCrypt.Net.BCrypt.Verify("newpassword", user.PasswordHash));
        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.PasswordResetTokenExpiresAt);
    }

    private static string ExtractToken(string? url)
    {
        Assert.NotNull(url);
        var uri = new Uri(url);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        return query["token"].ToString();
    }

    private class FakeRealtimeNotifier : IRealtimeNotifier
    {
        public Task AppChangedAsync(string domain, string action, Guid? userId = null, object? data = null)
            => Task.CompletedTask;
    }

    private class FakeEmailSender : IEmailSender
    {
        public string? LastEmail { get; private set; }
        public string? LastVerificationUrl { get; private set; }
        public string? LastPasswordResetEmail { get; private set; }
        public string? LastPasswordResetUrl { get; private set; }

        public Task SendEmailVerificationAsync(string email, string username, string verificationUrl)
        {
            LastEmail = email;
            LastVerificationUrl = verificationUrl;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string username, string resetUrl)
        {
            LastPasswordResetEmail = email;
            LastPasswordResetUrl = resetUrl;
            return Task.CompletedTask;
        }
    }
}
