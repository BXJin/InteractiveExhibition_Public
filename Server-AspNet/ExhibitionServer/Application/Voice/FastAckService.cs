using ExhibitionServer.Application.Abstractions;
using ExhibitionServer.Application.Knowledge;

namespace ExhibitionServer.Application.Voice;

/// <summary>
/// transcript를 분석해 적절한 캐시된 Ack 오디오를 즉시 반환한다.
/// OpenAI TTS 호출 없이 바로 재생해 지연 시간을 줄이는 목적.
///
/// ack wav 파일은 Data/TtsCache/ 에 미리 생성해 둔다.
/// 파일이 없으면 null 반환 (graceful degradation).
/// </summary>
public sealed class FastAckService
{
    private readonly IExhibitionKnowledgeStore _knowledgeStore;
    private readonly ILogger<FastAckService> _logger;

    private readonly IReadOnlyDictionary<AckKind, (byte[]? Audio, string Text)> _cache;

    public FastAckService(
        IExhibitionKnowledgeStore knowledgeStore,
        IWebHostEnvironment env,
        ILogger<FastAckService> logger)
    {
        _knowledgeStore = knowledgeStore;
        _logger = logger;
        _cache = LoadCache(env.ContentRootPath);
    }

    /// <summary>transcript에 맞는 Ack를 선택한다. 오디오가 없으면 Audio=null.</summary>
    public (byte[]? Audio, string Text)? Select(string transcript)
    {
        var kind = ClassifyTranscript(transcript);
        if (_cache.TryGetValue(kind, out var entry))
        {
            return entry;
        }

        return null;
    }

    // ── 분류 ─────────────────────────────────────────────────────────────────

    private AckKind ClassifyTranscript(string transcript)
    {
        var lower = transcript.ToLowerInvariant();

        if (ContainsAny(lower, "안녕", "반가", "hello", "hi"))
        {
            return AckKind.Greeting;
        }

        if (ContainsAny(lower, "전시관", "구역", "어디", "어느", "위치", "구성"))
        {
            return AckKind.Zone;
        }

        // 전시물 매칭 시도
        var results = _knowledgeStore.Search(transcript, selectedArtifactId: null, maxResults: 1);
        if (results.Count > 0)
        {
            return AckKind.Found;
        }

        return AckKind.Searching;
    }

    // ── 캐시 로드 ─────────────────────────────────────────────────────────────

    private IReadOnlyDictionary<AckKind, (byte[]? Audio, string Text)> LoadCache(string contentRoot)
    {
        var cacheDir = Path.Combine(contentRoot, "Data", "TtsCache");

        var entries = new Dictionary<AckKind, (byte[]? Audio, string Text)>
        {
            [AckKind.Found]     = (LoadFile(cacheDir, "ack_found.wav"),     "네, 설명해드릴게요."),
            [AckKind.Searching] = (LoadFile(cacheDir, "ack_searching.wav"), "잠시만요, 확인해볼게요."),
            [AckKind.Greeting]  = (LoadFile(cacheDir, "ack_greeting.wav"),  "안녕하세요."),
            [AckKind.Zone]      = (LoadFile(cacheDir, "ack_zone.wav"),      "네, 전시관 구성을 알려드릴게요.")
        };

        var loaded = entries.Values.Count(e => e.Audio != null);
        _logger.LogInformation("FastAck cache loaded. {Loaded}/{Total} wav files found.", loaded, entries.Count);

        return entries;
    }

    private byte[]? LoadFile(string dir, string fileName)
    {
        var path = Path.Combine(dir, fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return File.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to load ack file {FileName}: {Message}", fileName, ex.Message);
            return null;
        }
    }

    private static bool ContainsAny(string text, params string[] values) =>
        values.Any(v => text.Contains(v, StringComparison.OrdinalIgnoreCase));

    private enum AckKind { Found, Searching, Greeting, Zone }
}
