using System.Collections.Concurrent;
using Exhibition.Shared.Ai;
using ExhibitionServer.Application.Abstractions;

namespace ExhibitionServer.Application.Chat;

public sealed class ConversationMemoryStore : IConversationMemoryStore
{
    private const int MaxTurnsPerConversation = 10;
    private const int MaxContentLength = 800;
    private const int MaxConversationCount = 200;

    private readonly ConcurrentDictionary<string, ConversationBuffer> _buffers = new(StringComparer.Ordinal);

    public IReadOnlyList<AiConversationTurnDto> GetRecentTurns(string? conversationId)
    {
        var key = NormalizeConversationId(conversationId);
        if (key is null || !_buffers.TryGetValue(key, out var buffer))
        {
            return [];
        }

        lock (buffer.SyncRoot)
        {
            return buffer.Turns.ToArray();
        }
    }

    public void AppendExchange(string? conversationId, string userMessage, string assistantReply)
    {
        var key = NormalizeConversationId(conversationId);
        if (key is null)
        {
            return;
        }

        TrimConversationCountIfNeeded();

        var buffer = _buffers.GetOrAdd(key, _ => new ConversationBuffer());
        lock (buffer.SyncRoot)
        {
            buffer.Turns.Add(new AiConversationTurnDto
            {
                Role = "user",
                Content = TrimContent(userMessage)
            });
            buffer.Turns.Add(new AiConversationTurnDto
            {
                Role = "assistant",
                Content = TrimContent(assistantReply)
            });

            while (buffer.Turns.Count > MaxTurnsPerConversation)
            {
                buffer.Turns.RemoveAt(0);
            }
        }
    }

    private void TrimConversationCountIfNeeded()
    {
        if (_buffers.Count <= MaxConversationCount)
        {
            return;
        }

        foreach (var key in _buffers.Keys.Take(Math.Max(1, _buffers.Count - MaxConversationCount)))
        {
            _buffers.TryRemove(key, out _);
        }
    }

    private static string? NormalizeConversationId(string? conversationId)
    {
        var trimmed = conversationId?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        return trimmed.Length <= 120 ? trimmed : trimmed[..120];
    }

    private static string TrimContent(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= MaxContentLength ? trimmed : trimmed[..MaxContentLength];
    }

    private sealed class ConversationBuffer
    {
        public object SyncRoot { get; } = new();

        public List<AiConversationTurnDto> Turns { get; } = [];
    }
}
