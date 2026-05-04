namespace LootNet_API.Tests;

using LootNet_API.Hubs;
using LootNet_API.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;

public class SignalRRealtimeNotifierTests
{
    [Fact]
    public async Task AppChangedAsync_SendsAppStateChangedToAll_AndUserStateChangedToUser_WhenUserIdProvided()
    {
        var allProxy = new Mock<IClientProxy>();
        var userProxy = new Mock<IClientProxy>();
        var clients = new Mock<IHubClients>();
        var hub = new Mock<IHubContext<GameHub>>();

        clients.Setup(x => x.All).Returns(allProxy.Object);
        clients.Setup(x => x.User(It.IsAny<string>())).Returns(userProxy.Object);
        hub.Setup(x => x.Clients).Returns(clients.Object);

        var notifier = new SignalRRealtimeNotifier(hub.Object);
        var userId = Guid.NewGuid();

        await notifier.AppChangedAsync("run", "started", userId, new { runId = Guid.NewGuid() });

        allProxy.Verify(x => x.SendCoreAsync(
            "AppStateChanged",
            It.Is<object[]>(a => a.Length == 1),
            default), Times.Once);

        userProxy.Verify(x => x.SendCoreAsync(
            "UserStateChanged",
            It.Is<object[]>(a => a.Length == 1),
            default), Times.Once);
    }

    [Fact]
    public async Task AppChangedAsync_SendsOnlyAppStateChangedToAll_WhenUserIdMissing()
    {
        var allProxy = new Mock<IClientProxy>();
        var clients = new Mock<IHubClients>();
        var hub = new Mock<IHubContext<GameHub>>();

        clients.Setup(x => x.All).Returns(allProxy.Object);
        hub.Setup(x => x.Clients).Returns(clients.Object);

        var notifier = new SignalRRealtimeNotifier(hub.Object);

        await notifier.AppChangedAsync("market", "listing-created");

        allProxy.Verify(x => x.SendCoreAsync(
            "AppStateChanged",
            It.Is<object[]>(a => a.Length == 1),
            default), Times.Once);

        clients.Verify(x => x.User(It.IsAny<string>()), Times.Never);
    }
}

