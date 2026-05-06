namespace LootNet_API.Tests;

using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Services;
using LootNet_API.Services.Interfaces;
using LootNet_API.Tests.Helpers;
using Moq;
using Xunit;

public class ChatServiceTests
{
    private static User CreateUser()
    {
        var id = Guid.NewGuid();
        return new User
        {
            Id = id,
            Username = $"user_{id}",
            PasswordHash = "hash",
            Currency = 10,
            Role = UserRole.Player,
            Equipment = new Equipment { Id = Guid.NewGuid(), UserId = id }
        };
    }

    [Fact]
    public async Task SendGlobalMessage_SavesMessage()
    {
        var (db, _) = DbHelper.Create();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var notifier = new Mock<IRealtimeNotifier>();
        notifier.Setup(x => x.AppChangedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);

        var service = new ChatService(db, notifier.Object);
        var msg = await service.SendGlobalMessageAsync(user.Id, "hello world");

        Assert.Equal("hello world", msg.Text);
        Assert.Single(db.ChatMessages);
    }

    [Fact]
    public async Task SendPrivateMessage_SavesForParticipants()
    {
        var (db, _) = DbHelper.Create();
        var sender = CreateUser();
        var recipient = CreateUser();
        db.Users.AddRange(sender, recipient);
        await db.SaveChangesAsync();

        var notifier = new Mock<IRealtimeNotifier>();
        notifier.Setup(x => x.AppChangedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);

        var service = new ChatService(db, notifier.Object);
        await service.SendPrivateMessageAsync(sender.Id, recipient.Id, "private message");
        var thread = await service.GetPrivateMessagesAsync(sender.Id, recipient.Id, 1, 20);

        Assert.Single(thread.Items);
        Assert.Equal("private message", thread.Items[0].Text);
    }

    [Fact]
    public async Task SendMessage_ThrowsForEmptyText()
    {
        var (db, _) = DbHelper.Create();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var notifier = new Mock<IRealtimeNotifier>();
        notifier.Setup(x => x.AppChangedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);

        var service = new ChatService(db, notifier.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendGlobalMessageAsync(user.Id, "   "));
    }

    [Fact]
    public async Task GetPrivateConversations_ReturnsDistinctPartners()
    {
        var (db, _) = DbHelper.Create();
        var a = CreateUser();
        var b = CreateUser();
        var c = CreateUser();
        db.Users.AddRange(a, b, c);
        db.ChatMessages.AddRange(
            new ChatMessage { Id = Guid.NewGuid(), SenderId = a.Id, RecipientId = b.Id, Text = "1", CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new ChatMessage { Id = Guid.NewGuid(), SenderId = b.Id, RecipientId = a.Id, Text = "2", CreatedAt = DateTime.UtcNow.AddMinutes(-8) },
            new ChatMessage { Id = Guid.NewGuid(), SenderId = c.Id, RecipientId = a.Id, Text = "3", CreatedAt = DateTime.UtcNow.AddMinutes(-5) }
        );
        await db.SaveChangesAsync();

        var notifier = new Mock<IRealtimeNotifier>();
        notifier.Setup(x => x.AppChangedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);

        var service = new ChatService(db, notifier.Object);
        var conv = await service.GetPrivateConversationsAsync(a.Id);
        Assert.Equal(2, conv.Count);
    }
}
