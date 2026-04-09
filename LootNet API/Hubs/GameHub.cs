namespace LootNet_API.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class GameHub : Hub
{
    public async Task ItemGenerated(string itemJson)
    {
        await Clients.All.SendAsync("ItemGenerated", itemJson);
    }

    public async Task MarketItemListed(string listingJson)
    {
        await Clients.All.SendAsync("MarketItemListed", listingJson);
    }
}
