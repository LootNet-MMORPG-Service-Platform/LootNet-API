namespace LootNet_API.Tests;

using LootNet_API.Data;
using LootNet_API.Enums;
using LootNet_API.Models;
using LootNet_API.Models.Items;
using LootNet_API.Services;
using LootNet_API.Services.Interfaces;
using LootNet_API.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;

public class ProfileServiceTests
{
    private static User CreateUser()
    {
        var id = Guid.NewGuid();
        return new User
        {
            Id = id,
            Username = $"user_{id}",
            PasswordHash = "hash",
            Role = UserRole.Player,
            Currency = 100,
            Equipment = new Equipment { Id = Guid.NewGuid(), UserId = id }
        };
    }

    private static IFormFile CreatePngFile(string name = "pfp.png")
    {
        var bytes = CreatePng(256, 256);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = "image/png" };
    }

    [Fact]
    public async Task UploadProfilePicture_SavesPath_AndFile()
    {
        var (db, _) = DbHelper.Create();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var webRoot = Path.Combine(Path.GetTempPath(), $"lootnet-pfp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(webRoot);

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(x => x.WebRootPath).Returns(webRoot);

        var notifier = new Mock<IRealtimeNotifier>();
        notifier.Setup(x => x.AppChangedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);

        var service = new ProfileService(db, env.Object, notifier.Object);
        var path = await service.UploadProfilePictureAsync(user.Id, CreatePngFile());

        Assert.StartsWith("/uploads/pfp/", path);
        Assert.NotNull((await db.Users.FindAsync(user.Id))?.ProfileImagePath);

        var localFile = Path.Combine(webRoot, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(localFile));
    }

    [Fact]
    public async Task UploadProfilePicture_Throws_WhenWrongType()
    {
        var (db, _) = DbHelper.Create();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "x.gif")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/gif"
        };

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(x => x.WebRootPath).Returns(Path.GetTempPath());
        var notifier = new Mock<IRealtimeNotifier>();
        notifier.Setup(x => x.AppChangedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);

        var service = new ProfileService(db, env.Object, notifier.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadProfilePictureAsync(user.Id, file));
    }

    [Fact]
    public async Task UploadProfilePicture_Throws_WhenImageTooSmall()
    {
        var (db, _) = DbHelper.Create();
        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var bytes = CreatePng(64, 64);
        var stream = new MemoryStream(bytes);
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "small.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(x => x.WebRootPath).Returns(Path.GetTempPath());
        var notifier = new Mock<IRealtimeNotifier>();
        notifier.Setup(x => x.AppChangedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);

        var service = new ProfileService(db, env.Object, notifier.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadProfilePictureAsync(user.Id, file));
    }

    private static byte[] CreatePng(int width, int height)
    {
        var bytes = new byte[24];
        bytes[0] = 0x89; bytes[1] = 0x50; bytes[2] = 0x4E; bytes[3] = 0x47;
        bytes[4] = 0x0D; bytes[5] = 0x0A; bytes[6] = 0x1A; bytes[7] = 0x0A;
        bytes[8] = 0x00; bytes[9] = 0x00; bytes[10] = 0x00; bytes[11] = 0x0D;
        bytes[12] = 0x49; bytes[13] = 0x48; bytes[14] = 0x44; bytes[15] = 0x52;
        bytes[16] = (byte)((width >> 24) & 0xFF);
        bytes[17] = (byte)((width >> 16) & 0xFF);
        bytes[18] = (byte)((width >> 8) & 0xFF);
        bytes[19] = (byte)(width & 0xFF);
        bytes[20] = (byte)((height >> 24) & 0xFF);
        bytes[21] = (byte)((height >> 16) & 0xFF);
        bytes[22] = (byte)((height >> 8) & 0xFF);
        bytes[23] = (byte)(height & 0xFF);
        return bytes;
    }
}
