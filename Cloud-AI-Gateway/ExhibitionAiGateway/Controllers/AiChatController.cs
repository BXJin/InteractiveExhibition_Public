using Exhibition.Shared.Ai;
using ExhibitionAiGateway.Application.Abstractions;
using ExhibitionAiGateway.Streaming;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExhibitionAiGateway.Controllers;

[ApiController]
[Route("api/ai/chat")]
[EnableRateLimiting("ai-chat")]
public sealed class AiChatController : ControllerBase
{
    private readonly IAiChatService _aiChatService;
    private readonly IAiChatStreamService _aiChatStreamService;

    public AiChatController(
        IAiChatService aiChatService,
        IAiChatStreamService aiChatStreamService)
    {
        _aiChatService = aiChatService;
        _aiChatStreamService = aiChatStreamService;
    }

    [HttpPost]
    public async Task<ActionResult<AiChatResponse>> CreateReply(
        [FromBody] AiChatRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _aiChatService.CreateReplyAsync(request, cancellationToken);

        if (response.Success)
        {
            return Ok(response);
        }

        return response.ErrorCode switch
        {
            "EMPTY_MESSAGE" or "MESSAGE_TOO_LONG" => BadRequest(response),
            not null when response.ErrorCode.EndsWith("_MISSING", StringComparison.OrdinalIgnoreCase) =>
                StatusCode(StatusCodes.Status503ServiceUnavailable, response),
            not null when response.ErrorCode.EndsWith("_NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase) =>
                StatusCode(StatusCodes.Status503ServiceUnavailable, response),
            _ => StatusCode(StatusCodes.Status502BadGateway, response)
        };
    }

    [HttpPost("stream")]
    public async Task StreamReply(
        [FromBody] AiChatRequest request,
        CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await foreach (var streamEvent in _aiChatStreamService.StreamReplyAsync(request, cancellationToken))
        {
            await SseWriter.WriteEventAsync(Response, streamEvent, cancellationToken);
        }
    }
}
