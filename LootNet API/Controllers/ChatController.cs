using LootNet_API.DTO;
using LootNet_API.Extensions;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LootNet_API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("global")]
    public async Task<IActionResult> GetGlobal([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 30)
    {
        var result = await _chatService.GetGlobalMessagesAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("private/conversations")]
    public async Task<IActionResult> GetPrivateConversations()
    {
        var userId = User.GetUserId();
        var result = await _chatService.GetPrivateConversationsAsync(userId);
        return Ok(result);
    }

    [HttpPost("global")]
    public async Task<IActionResult> SendGlobal([FromBody] SendGlobalChatMessageDTO dto)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _chatService.SendGlobalMessageAsync(userId, dto.Text);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("private")]
    public async Task<IActionResult> GetPrivate([FromBody] PrivateChatQueryDTO dto)
    {
        var userId = User.GetUserId();
        var result = await _chatService.GetPrivateMessagesAsync(userId, dto.OtherUserId, dto.PageNumber, dto.PageSize);
        return Ok(result);
    }

    [HttpPost("private/send")]
    public async Task<IActionResult> SendPrivate([FromBody] SendPrivateChatMessageDTO dto)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _chatService.SendPrivateMessageAsync(userId, dto.RecipientId, dto.Text);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
