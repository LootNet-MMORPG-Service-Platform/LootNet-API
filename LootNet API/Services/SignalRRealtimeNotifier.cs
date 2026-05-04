using LootNet_API.Hubs;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LootNet_API.Services;

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<GameHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<GameHub> hub)
    {
        _hub = hub;
    }

    public async Task AppChangedAsync(string domain, string action, Guid? userId = null, object? data = null)
    {
        var payload = new
        {
            domain,
            action,
            userId,
            data,
            at = DateTime.UtcNow
        };

        await _hub.Clients.All.SendAsync("AppStateChanged", payload);

        if (userId.HasValue)
            await _hub.Clients.User(userId.Value.ToString()).SendAsync("UserStateChanged", payload);
    }
}
