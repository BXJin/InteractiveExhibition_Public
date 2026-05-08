using System.Collections.Concurrent;

namespace ExhibitionServer.Application.Voice;

/// <summary>
/// TTS 오디오 바이트를 임시로 인메모리에 저장한다.
/// 패널이 audioUrl로 오디오를 fetch하면 서버는 이 Store에서 꺼내 반환한다.
/// ExpiryMinutes 이후 자동 만료 (lazy cleanup).
/// </summary>
public sealed class TtsAudioStore
{
    private const int ExpiryMinutes = 10;

    private readonly ConcurrentDictionary<string, AudioEntry> _store = new();

    /// <summary>오디오 바이트를 저장하고 고유 ID를 반환한다.</summary>
    public string Store(byte[] audioBytes)
    {
        CleanupExpired();

        var id = Guid.NewGuid().ToString("n");
        _store[id] = new AudioEntry(audioBytes, DateTime.UtcNow);
        return id;
    }

    /// <summary>ID로 오디오 바이트를 꺼낸다. 없거나 만료됐으면 null 반환.</summary>
    public byte[]? Get(string id)
    {
        if (!_store.TryGetValue(id, out var entry))
        {
            return null;
        }

        if (IsExpired(entry))
        {
            _store.TryRemove(id, out _);
            return null;
        }

        return entry.Data;
    }

    private void CleanupExpired()
    {
        foreach (var (key, entry) in _store)
        {
            if (IsExpired(entry))
            {
                _store.TryRemove(key, out _);
            }
        }
    }

    private static bool IsExpired(AudioEntry entry) =>
        DateTime.UtcNow - entry.CreatedAt > TimeSpan.FromMinutes(ExpiryMinutes);

    private sealed record AudioEntry(byte[] Data, DateTime CreatedAt);
}
