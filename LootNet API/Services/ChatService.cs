using LootNet_API.Data;
using LootNet_API.DTO;
using LootNet_API.Models;
using LootNet_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LootNet_API.Services;

public class ChatService : IChatService
{
    private readonly AppDbContext _context;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public ChatService(AppDbContext context, IRealtimeNotifier realtimeNotifier)
    {
        _context = context;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<PagedResultDTO<ChatMessageDTO>> GetGlobalMessagesAsync(int pageNumber, int pageSize)
    {
        var q = _context.ChatMessages.Where(x => x.RecipientId == null).OrderByDescending(x => x.CreatedAt);
        return await QueryMessagesAsync(q, pageNumber, pageSize);
    }

    public async Task<List<ChatConversationDTO>> GetPrivateConversationsAsync(Guid userId)
    {
        var messages = await _context.ChatMessages
            .Where(x => x.RecipientId != null && (x.SenderId == userId || x.RecipientId == userId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var partnerIds = messages
            .Select(x => x.SenderId == userId ? x.RecipientId!.Value : x.SenderId)
            .Distinct()
            .ToList();

        var users = await _context.Users.Where(x => partnerIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Username, x.ProfileImagePath })
            .ToListAsync();

        return partnerIds.Select(pid =>
        {
            var last = messages.First(m => (m.SenderId == userId && m.RecipientId == pid) || (m.SenderId == pid && m.RecipientId == userId));
            var u = users.FirstOrDefault(x => x.Id == pid);
            return new ChatConversationDTO
            {
                UserId = pid,
                Username = u?.Username ?? "Unknown",
                ProfileImagePath = u?.ProfileImagePath,
                LastMessageText = last.Text,
                LastMessageAt = last.CreatedAt
            };
        }).OrderByDescending(x => x.LastMessageAt).ToList();
    }

    public async Task<PagedResultDTO<ChatMessageDTO>> GetPrivateMessagesAsync(Guid userId, Guid otherUserId, int pageNumber, int pageSize)
    {
        var q = _context.ChatMessages
            .Where(x => x.RecipientId != null &&
                        ((x.SenderId == userId && x.RecipientId == otherUserId) ||
                         (x.SenderId == otherUserId && x.RecipientId == userId)))
            .OrderByDescending(x => x.CreatedAt);
        return await QueryMessagesAsync(q, pageNumber, pageSize);
    }

    public async Task<ChatMessageDTO> SendGlobalMessageAsync(Guid senderId, string text)
    {
        var sender = await _context.Users.FirstOrDefaultAsync(x => x.Id == senderId)
            ?? throw new InvalidOperationException("Sender not found.");
        var normalized = NormalizeText(text);

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            RecipientId = null,
            Text = normalized
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        var dto = ToDto(message, sender.Username, sender.ProfileImagePath);
        await _realtimeNotifier.AppChangedAsync("chat", "global-message", null, dto);
        return dto;
    }

    public async Task<ChatMessageDTO> SendPrivateMessageAsync(Guid senderId, Guid recipientId, string text)
    {
        if (senderId == recipientId)
            throw new InvalidOperationException("Cannot send private message to yourself.");

        var sender = await _context.Users.FirstOrDefaultAsync(x => x.Id == senderId)
            ?? throw new InvalidOperationException("Sender not found.");
        var recipient = await _context.Users.FirstOrDefaultAsync(x => x.Id == recipientId)
            ?? throw new InvalidOperationException("Recipient not found.");
        var normalized = NormalizeText(text);

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            RecipientId = recipient.Id,
            Text = normalized
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        var dto = ToDto(message, sender.Username, sender.ProfileImagePath);
        await _realtimeNotifier.AppChangedAsync("chat", "private-message", recipient.Id, dto);
        await _realtimeNotifier.AppChangedAsync("chat", "private-message", senderId, dto);
        return dto;
    }

    private async Task<PagedResultDTO<ChatMessageDTO>> QueryMessagesAsync(IQueryable<ChatMessage> q, int pageNumber, int pageSize)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 30;

        var total = await q.CountAsync();
        var messages = await q.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        var senderIds = messages.Select(x => x.SenderId).Distinct().ToList();
        var senders = await _context.Users.Where(x => senderIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Username, x.ProfileImagePath }).ToListAsync();

        var items = messages
            .Select(x =>
            {
                var s = senders.FirstOrDefault(u => u.Id == x.SenderId);
                return ToDto(x, s?.Username ?? "Unknown", s?.ProfileImagePath);
            })
            .OrderBy(x => x.CreatedAt)
            .ToList();

        return new PagedResultDTO<ChatMessageDTO> { Items = items, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    private static ChatMessageDTO ToDto(ChatMessage msg, string username, string? pfp) => new()
    {
        Id = msg.Id,
        SenderId = msg.SenderId,
        SenderUsername = username,
        SenderProfileImagePath = pfp,
        RecipientId = msg.RecipientId,
        Text = msg.Text,
        CreatedAt = msg.CreatedAt
    };

    private static string NormalizeText(string input)
    {
        var text = (input ?? string.Empty).Trim();
        if (text.Length == 0) throw new InvalidOperationException("Message cannot be empty.");
        if (text.Length > 500) throw new InvalidOperationException("Message is too long.");
        return text;
    }
}
