using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LootNet_API.Services;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public ProfileService(AppDbContext context, IWebHostEnvironment environment, IRealtimeNotifier realtimeNotifier)
    {
        _context = context;
        _environment = environment;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<UserProfileDTO> GetProfileAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        return new UserProfileDTO
        {
            Username = user.Username,
            Currency = user.Currency,
            Role = user.Role,
            ProfileImagePath = user.ProfileImagePath
        };
    }

    public async Task<string> UploadProfilePictureAsync(Guid userId, IFormFile file)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File is required.");

        var allowedTypes = new[] { "image/png", "image/jpeg", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Only png/jpeg/webp are allowed.");

        await using var probeStream = file.OpenReadStream();
        var dimensions = TryReadImageDimensions(probeStream, file.ContentType);
        if (dimensions == null)
            throw new InvalidOperationException("Could not read image dimensions.");

        const int minSide = 128;
        const int maxSide = 4096;
        if (dimensions.Value.Width < minSide || dimensions.Value.Height < minSide)
            throw new InvalidOperationException($"Image is too small. Minimum size is {minSide}x{minSide}px.");
        if (dimensions.Value.Width > maxSide || dimensions.Value.Height > maxSide)
            throw new InvalidOperationException($"Image is too large. Maximum size is {maxSide}x{maxSide}px.");

        var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "pfp");
        Directory.CreateDirectory(uploadsDir);

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = file.ContentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
        }

        var fileName = $"{userId:N}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension}";
        var fullPath = Path.Combine(uploadsDir, fileName);

        await using (var stream = File.Create(fullPath))
            await file.CopyToAsync(stream);

        user.ProfileImagePath = $"/uploads/pfp/{fileName}";
        await _context.SaveChangesAsync();

        await _realtimeNotifier.AppChangedAsync("profile", "pfp-updated", userId, new { user.ProfileImagePath });
        return user.ProfileImagePath;
    }

    private static (int Width, int Height)? TryReadImageDimensions(Stream stream, string contentType)
    {
        if (!stream.CanSeek) return null;
        stream.Position = 0;
        return contentType switch
        {
            "image/png" => TryReadPngDimensions(stream),
            "image/jpeg" => TryReadJpegDimensions(stream),
            "image/webp" => TryReadWebpDimensions(stream),
            _ => null
        };
    }

    private static (int Width, int Height)? TryReadPngDimensions(Stream stream)
    {
        Span<byte> header = stackalloc byte[24];
        if (stream.Read(header) < 24) return null;
        var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        if (!isPng) return null;
        var width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
        var height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
        return width > 0 && height > 0 ? (width, height) : null;
    }

    private static (int Width, int Height)? TryReadJpegDimensions(Stream stream)
    {
        int ReadByteSafe() => stream.ReadByte();
        if (ReadByteSafe() != 0xFF || ReadByteSafe() != 0xD8) return null;

        while (true)
        {
            int markerPrefix;
            do markerPrefix = ReadByteSafe(); while (markerPrefix == 0xFF);
            if (markerPrefix < 0) return null;

            int marker = markerPrefix;
            if (marker == 0xD9 || marker == 0xDA) return null;

            var lenHi = ReadByteSafe();
            var lenLo = ReadByteSafe();
            if (lenHi < 0 || lenLo < 0) return null;
            var segmentLength = (lenHi << 8) + lenLo;
            if (segmentLength < 2) return null;

            if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
            {
                _ = ReadByteSafe();
                var hHi = ReadByteSafe();
                var hLo = ReadByteSafe();
                var wHi = ReadByteSafe();
                var wLo = ReadByteSafe();
                if (hHi < 0 || hLo < 0 || wHi < 0 || wLo < 0) return null;
                var height = (hHi << 8) + hLo;
                var width = (wHi << 8) + wLo;
                return width > 0 && height > 0 ? (width, height) : null;
            }

            stream.Position += segmentLength - 2;
        }
    }

    private static (int Width, int Height)? TryReadWebpDimensions(Stream stream)
    {
        using var br = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var riff = new string(br.ReadChars(4));
        _ = br.ReadUInt32();
        var webp = new string(br.ReadChars(4));
        if (riff != "RIFF" || webp != "WEBP") return null;

        while (stream.Position + 8 <= stream.Length)
        {
            var chunk = new string(br.ReadChars(4));
            var size = br.ReadUInt32();
            if (chunk == "VP8X")
            {
                _ = br.ReadByte();
                _ = br.ReadBytes(3);
                var w = br.ReadBytes(3);
                var h = br.ReadBytes(3);
                var width = 1 + w[0] + (w[1] << 8) + (w[2] << 16);
                var height = 1 + h[0] + (h[1] << 8) + (h[2] << 16);
                return width > 0 && height > 0 ? (width, height) : null;
            }

            stream.Position += size + (size % 2);
        }

        return null;
    }
}
