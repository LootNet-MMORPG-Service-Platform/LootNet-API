using LootNet_API.Data;
using LootNet_API.Enums;
using LootNet_API.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LootNet_API.Services.Interfaces;

namespace LootNet_API.Tests.Integration;
public class GameHubIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GameHubIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });
    }

    [Fact]
    public async Task Hub_Should_Send_And_Receive_Event()
    {
        var client = _factory.CreateClient();

        Guid userId;
        string jwtToken;
        string refreshToken;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = "fakehash",
                Role = UserRole.Player,
                Currency = 1000,
                Equipment = new Equipment()
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            userId = user.Id;

            jwtToken = tokenService.GenerateJwt(user);
            var refresh = tokenService.GenerateRefreshToken(user.Id);
            refreshToken = refresh.Token;
        }

        var connection = new HubConnectionBuilder()
            .WithUrl("https://localhost/hub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult(jwtToken);
            })
            .WithAutomaticReconnect()
            .Build();

        var tcs = new TaskCompletionSource<string>();

        connection.On<string>("ItemGenerated", msg =>
        {
            tcs.SetResult(msg);
        });

        await connection.StartAsync();

        await connection.InvokeAsync("ItemGenerated", "Test Sword");

        var result = await tcs.Task;
        Assert.Contains("Test Sword", result);

        await connection.DisposeAsync();
    }
}
