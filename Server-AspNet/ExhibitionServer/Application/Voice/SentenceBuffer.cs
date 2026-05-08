using System.Text;

namespace ExhibitionServer.Application.Voice;

/// <summary>
/// LLM 스트리밍 delta 청크를 누적하고 문장 단위로 분리한다.
/// 문장 경계: . ? ! 。 ？ ！ 뒤에 공백 또는 스트림 종료.
/// 폴백: 쉼표(, ，) 이후 누적 길이가 FallbackMinLength 이상이면 분리.
/// </summary>
public sealed class SentenceBuffer
{
    private const int FallbackMinLength = 100;
    private const int MinSentenceLength  = 10;

    private readonly StringBuilder _buffer = new();

    public void Feed(string chunk)
    {
        _buffer.Append(chunk);
    }

    /// <summary>
    /// 완성된 문장이 있으면 꺼낸다. 없으면 false 반환.
    /// </summary>
    public bool TryFlushSentence(out string sentence)
    {
        var text = _buffer.ToString();

        var idx = FindSentenceEnd(text);
        if (idx >= 0)
        {
            sentence = text[..(idx + 1)].Trim();
            _buffer.Remove(0, idx + 1);
            // 문장 앞의 공백 제거
            var leadingSpaces = 0;
            while (leadingSpaces < _buffer.Length && _buffer[leadingSpaces] == ' ')
            {
                leadingSpaces++;
            }
            if (leadingSpaces > 0)
            {
                _buffer.Remove(0, leadingSpaces);
            }

            // 너무 짧은 문장은 다음 문장과 합산될 때까지 대기
            if (sentence.Length < MinSentenceLength && _buffer.Length > 0)
            {
                _buffer.Insert(0, sentence + " ");
                sentence = string.Empty;
                return false;
            }

            return !string.IsNullOrWhiteSpace(sentence);
        }

        // 폴백: 쉼표 이후 버퍼가 충분히 길면 분리
        if (text.Length >= FallbackMinLength)
        {
            var commaIdx = FindLastComma(text);
            if (commaIdx > 0)
            {
                sentence = text[..(commaIdx + 1)].Trim();
                _buffer.Remove(0, commaIdx + 1);
                return !string.IsNullOrWhiteSpace(sentence);
            }
        }

        sentence = string.Empty;
        return false;
    }

    /// <summary>남은 내용을 전부 꺼낸다 (스트림 종료 시 호출).</summary>
    public string FlushRemaining()
    {
        var text = _buffer.ToString().Trim();
        _buffer.Clear();
        return text;
    }

    public bool IsEmpty => _buffer.Length == 0;

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────────

    private static int FindSentenceEnd(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (!IsSentenceTerminator(text[i]))
            {
                continue;
            }

            // 연속 종결 부호 처리 ("!!", "?!")
            var end = i;
            while (end + 1 < text.Length && IsSentenceTerminator(text[end + 1]))
            {
                end++;
            }

            // 종결 부호 뒤에 공백이 있거나 문자열 끝이면 분리
            if (end + 1 >= text.Length || text[end + 1] == ' ' || text[end + 1] == '\n')
            {
                return end;
            }
        }

        return -1;
    }

    private static int FindLastComma(string text)
    {
        for (var i = text.Length - 1; i >= 0; i--)
        {
            if (text[i] == ',' || text[i] == '，')
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsSentenceTerminator(char c) =>
        c is '.' or '?' or '!' or '。' or '？' or '！';
}
