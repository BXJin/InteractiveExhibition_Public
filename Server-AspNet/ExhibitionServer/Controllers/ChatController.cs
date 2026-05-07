using ExhibitionServer.Application.Abstractions;
using ExhibitionServer.Application.Chat;
using ExhibitionServer.Streaming;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionServer.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatGuideService _chatGuide;

    public ChatController(IChatGuideService chatGuide)
    {
        _chatGuide = chatGuide;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var response = await _chatGuide.ProcessAsync(request, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("stream")]
    public async Task Stream([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await foreach (var streamEvent in _chatGuide.StreamAsync(request, cancellationToken))
        {
            await SseWriter.WriteEventAsync(Response, streamEvent, cancellationToken);
        }
    }
}
