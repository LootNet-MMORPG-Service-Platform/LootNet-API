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
        var config = BuildConfig(development: true);

        _emailSender = new FakeEmailSender();
        _service = new AuthService(_db, config, new TokenService(_db, config), new FakeRealtimeNotifier(), _emailSender);
    }

    [Fact]
    public async Task RegisterAsync_AllowsWeakPassword_WhenDevelopmentEnabled()
    {
        await _service.RegisterAsync(new RegisterDTO { Username = "player1", Email = "player1@example.com", Password = "short" });

        Assert.True(await _db.Users.AnyAsync(u => u.Username == "player1"));
    }

    [Fact]
    public async Task RegisterAsync_RejectsWeakPassword_WhenDevelopmentDisabled()
    {
        var db = TestDbContextFactory.Create();
        var config = BuildConfig(development: false);
        var service = new AuthService(db, config, new TokenService(db, config), new FakeRealtimeNotifier(), new FakeEmailSender());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(new RegisterDTO { Username = "player1", Email = "player1@example.com", Password = "password" }));

        Assert.Equal("Password must be at least 12 characters.", ex.Message);
        Assert.False(await db.Users.AnyAsync());
    }

    [Fact]
    public async Task RegisterAsync_AllowsStrongPassword_WhenDevelopmentDisabled()
    {
        var db = TestDbContextFactory.Create();
        var config = BuildConfig(development: false);
        var service = new AuthService(db, config, new TokenService(db, config), new FakeRealtimeNotifier(), new FakeEmailSender());

        await service.RegisterAsync(new RegisterDTO { Username = "player1", Email = "player1@example.com", Password = "StrongPass1!" });

        Assert.True(await db.Users.AnyAsync(u => u.Username == "player1"));
    }

    private static IConfiguration BuildConfig(bool development)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            {"Jwt:Key", "SuperSecretKey12345678901234567890"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"},
            {"Jwt:AccessTokenMinutes", "60"},
            {"Jwt:RefreshTokenDays", "7"},
            {"App:PublicBaseUrl", "https://lootnet-api.test"},
            {"App:WebBaseUrl", "https://lootnet-web.test"},
            {"App:MobileVerificationBaseUrl", "lootnet://verify-email"},
            {"Development", development.ToString()}
        }).Build();
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
    public async Task RegisterAsync_UsesMobileVerificationUrl_WhenMobileClientRequested()
    {
        await _service.RegisterAsync(new RegisterDTO
        {
            Username = "player1",
            Email = "player1@example.com",
            Password = "password",
            VerificationClient = LootNet_API.Enums.EmailVerificationClient.Mobile
        });

        Assert.StartsWith("lootnet://verify-email?token=", _emailSender.LastVerificationUrl);
    }

    [Fact]
    public async Task RegisterAsync_DoesNotCreateUser_WhenVerificationEmailFails()
    {
        _emailSender.FailVerificationEmail = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RegisterAsync(new RegisterDTO { Username = "player1", Email = "player1@example.com", Password = "password" }));

        Assert.False(await _db.Users.AnyAsync());
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
    public async Task VerifyEmailAsync_DeletesUser_WhenVerificationTokenExpired()
    {
        await _service.RegisterAsync(new RegisterDTO { Username = "player1", Email = "player1@example.com", Password = "password" });
        var token = ExtractToken(_emailSender.LastVerificationUrl);
        var user = await _db.Users.SingleAsync();
        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyEmailAsync(token));

        Assert.Equal("Email verification token expired.", ex.Message);
        Assert.False(await _db.Users.AnyAsync());
    }

    [Fact]
    public async Task DeleteExpiredUnverifiedUsersAsync_RemovesExpiredUnverifiedUsers()
    {
        _db.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                Username = "expired",
                Email = "expired@example.com",
                EmailVerified = false,
                EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1),
                PasswordHash = "hash",
                Equipment = new Equipment()
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "verified",
                Email = "verified@example.com",
                EmailVerified = true,
                EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1),
                PasswordHash = "hash",
                Equipment = new Equipment()
            });
        await _db.SaveChangesAsync();

        var deleted = await _service.DeleteExpiredUnverifiedUsersAsync();

        Assert.Equal(1, deleted);
        Assert.False(await _db.Users.AnyAsync(u => u.Username == "expired"));
        Assert.True(await _db.Users.AnyAsync(u => u.Username == "verified"));
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
        public bool FailVerificationEmail { get; set; }

        public Task SendEmailVerificationAsync(string email, string username, string verificationUrl)
        {
            if (FailVerificationEmail)
                throw new InvalidOperationException("Email send failed.");

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
