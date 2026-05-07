using System.Text;

namespace ExhibitionAiGateway.Providers.OpenAI;

/// <summary>
/// Incrementally extracts the value of the "reply" field from a streaming JSON object.
/// Used to emit natural-language delta events before the full JSON is available.
/// </summary>
internal sealed class ReplyStreamExtractor
{
    private readonly StringBuilder _buffer = new();
    private int _replyStart = -1;
    private int _lastEmitted = 0;
    private bool _done = false;

    private const string ReplyKeyPattern = "\"reply\":";

    /// <summary>
    /// Appends a new delta chunk and returns the extracted reply text, or null if not yet available.
    /// </summary>
    public string? AddChunk(string chunk)
    {
        if (_done) return null;

        _buffer.Append(chunk);
        var text = _buffer.ToString();

        if (_replyStart < 0)
        {
            if (!TryLocateReplyValueStart(text, out var start))
                return null;

            _replyStart = start;
            _lastEmitted = start;
        }

        return ExtractNewReplyChars(text);
    }

    private static bool TryLocateReplyValueStart(string text, out int start)
    {
        start = -1;

        var keyIdx = text.IndexOf(ReplyKeyPattern, StringComparison.Ordinal);
        if (keyIdx < 0) return false;

        var cursor = keyIdx + ReplyKeyPattern.Length;
        while (cursor < text.Length && text[cursor] == ' ') cursor++;

        if (cursor >= text.Length || text[cursor] != '"') return false;

        start = cursor + 1;
        return true;
    }

    private string? ExtractNewReplyChars(string text)
    {
        var closeIdx = FindClosingQuote(text, _lastEmitted);

        if (closeIdx < 0)
        {
            if (text.Length <= _lastEmitted) return null;
            var partial = Unescape(text[_lastEmitted..]);
            _lastEmitted = text.Length;
            return partial.Length > 0 ? partial : null;
        }

        _done = true;
        if (closeIdx <= _lastEmitted) return null;

        var final = Unescape(text[_lastEmitted..closeIdx]);
        return final.Length > 0 ? final : null;
    }

    private static int FindClosingQuote(string text, int from)
    {
        for (var i = from; i < text.Length; i++)
        {
            if (text[i] != '"') continue;

            var backslashes = 0;
            for (var j = i - 1; j >= 0 && text[j] == '\\'; j--)
                backslashes++;

            if (backslashes % 2 == 0)
                return i;
        }

        return -1;
    }

    private static string Unescape(string value) =>
        value.Replace("\\\"", "\"")
             .Replace("\\n", "\n")
             .Replace("\\r", "\r")
             .Replace("\\t", "\t")
             .Replace("\\\\", "\\");
}
