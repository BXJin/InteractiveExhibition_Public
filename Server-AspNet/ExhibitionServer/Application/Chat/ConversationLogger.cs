using System.Text.Json;
using ExhibitionServer.Application.Abstractions;

namespace ExhibitionServer.Application.Chat;

public sealed class ConversationLogger : IConversationLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _logPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<ConversationLogger> _logger;

    public ConversationLogger(IHostEnvironment environment, ILogger<ConversationLogger> logger)
    {
        _logger = logger;
        var logDirectory = Path.Combine(environment.ContentRootPath, "Data", "Logs");
        Directory.CreateDirectory(logDirectory);
        _logPath = Path.Combine(logDirectory, "chat-log.jsonl");
    }

    public async Task LogAsync(ChatLogEntry entry, CancellationToken cancellationToken = default)
    {
        var line = JsonSerializer.Serialize(entry, JsonOptions);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_logPath, line + Environment.NewLine, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write chat log.");
        }
        finally
        {
            _lock.Release();
        }
    }
}
