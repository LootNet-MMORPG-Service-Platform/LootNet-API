using LootNet_API.DTO;

namespace LootNet_API.Services.Interfaces;

public interface IChatService
{
    Task<List<ChatConversationDTO>> GetPrivateConversationsAsync(Guid userId);
    Task<PagedResultDTO<ChatMessageDTO>> GetGlobalMessagesAsync(int pageNumber, int pageSize);
    Task<PagedResultDTO<ChatMessageDTO>> GetPrivateMessagesAsync(Guid userId, Guid otherUserId, int pageNumber, int pageSize);
    Task<ChatMessageDTO> SendGlobalMessageAsync(Guid senderId, string text);
    Task<ChatMessageDTO> SendPrivateMessageAsync(Guid senderId, Guid recipientId, string text);
}
