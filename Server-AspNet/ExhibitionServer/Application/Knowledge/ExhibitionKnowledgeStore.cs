using System.Text.Json;
using ExhibitionServer.Application.Abstractions;
using ExhibitionServer.Options;
using Microsoft.Extensions.Options;

namespace ExhibitionServer.Application.Knowledge;

public sealed class ExhibitionKnowledgeStore : IExhibitionKnowledgeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadOnlyList<ExhibitionKnowledgeDocument> _documents;
    private readonly IKnowledgeVectorSearch _vectorSearch;
    private readonly KnowledgeSearchOptions _searchOptions;
    private readonly ILogger<ExhibitionKnowledgeStore> _logger;

    public ExhibitionKnowledgeStore(
        IHostEnvironment environment,
        IKnowledgeVectorSearch vectorSearch,
        IOptions<KnowledgeSearchOptions> searchOptions,
        ILogger<ExhibitionKnowledgeStore> logger)
    {
        _vectorSearch = vectorSearch;
        _searchOptions = searchOptions.Value;
        _logger = logger;

        var directory = Path.Combine(environment.ContentRootPath, "Data", "ExhibitionKnowledge");
        Directory.CreateDirectory(directory);

        var documents = new List<ExhibitionKnowledgeDocument>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(path);
                var document = JsonSerializer.Deserialize<ExhibitionKnowledgeDocument>(json, JsonOptions);
                if (document is not null)
                {
                    documents.Add(document);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load knowledge document. Path={Path}", path);
            }
        }

        _documents = documents.Count > 0 ? documents : BuiltInDocuments();
    }

    public IReadOnlyList<ExhibitionKnowledgeDocument> GetCatalogDocuments(int maxResults = 10)
    {
        var limit = Math.Max(1, maxResults);

        return _documents
            .Where(document => document.CatalogVisible && !string.IsNullOrWhiteSpace(document.SourceUrl))
            .OrderBy(document => document.Zone, StringComparer.OrdinalIgnoreCase)
            .ThenBy(document => document.Title, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();
    }

    public IReadOnlyList<ExhibitionKnowledgeDocument> Search(string message, string? selectedArtifactId, int maxResults = 3)
    {
        var tokens = Tokenize(message);
        var keywordScores = _documents.ToDictionary(
            document => document.Id,
            document => Score(document, tokens, selectedArtifactId),
            StringComparer.Ordinal);

        var maxKeywordScore = Math.Max(1, keywordScores.Values.DefaultIfEmpty(0).Max());
        var vectorScores = SearchVectorScores(message, maxResults);

        var results = _documents.Select(document =>
            {
                var keywordScore = keywordScores[document.Id] / (double)maxKeywordScore;
                var vectorScore = vectorScores.GetValueOrDefault(document.Id);
                var selectedBoost = string.Equals(document.Id, selectedArtifactId, StringComparison.OrdinalIgnoreCase)
                    ? 1.0
                    : 0.0;

                var hybridScore =
                    (_searchOptions.KeywordWeight * keywordScore) +
                    (_searchOptions.VectorWeight * vectorScore) +
                    selectedBoost;

                return new
                {
                    Document = document,
                    KeywordScore = keywordScore,
                    VectorScore = vectorScore,
                    HybridScore = hybridScore
                };
            })
            .Where(x => x.HybridScore >= _searchOptions.MinHybridScore || keywordScores[x.Document.Id] > 0)
            .OrderByDescending(x => x.HybridScore)
            .ThenByDescending(x => x.VectorScore)
            .ThenBy(x => x.Document.Id)
            .Take(Math.Max(1, maxResults))
            .Select(x => x.Document)
            .ToArray();

        return results.Length > 0
            ? results
            : _documents.Take(Math.Max(1, maxResults)).ToArray();
    }

    private IReadOnlyDictionary<string, double> SearchVectorScores(string message, int maxResults)
    {
        if (!_vectorSearch.IsEnabled)
        {
            return new Dictionary<string, double>(StringComparer.Ordinal);
        }

        try
        {
            return _vectorSearch.SearchAsync(message, _documents, Math.Max(1, maxResults))
                .GetAwaiter()
                .GetResult()
                .ToDictionary(
                    result => result.Document.Id,
                    result => Math.Clamp(result.Score, 0, 1),
                    StringComparer.Ordinal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vector knowledge search failed. Falling back to keyword search.");
            return new Dictionary<string, double>(StringComparer.Ordinal);
        }
    }

    private static int Score(ExhibitionKnowledgeDocument document, IReadOnlySet<string> tokens, string? selectedArtifactId)
    {
        var score = string.Equals(document.Id, selectedArtifactId, StringComparison.OrdinalIgnoreCase) ? 20 : 0;
        var haystack = document.SearchText.ToLowerInvariant();

        foreach (var token in tokens)
        {
            if (haystack.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                score += document.Tags.Any(tag => string.Equals(tag, token, StringComparison.OrdinalIgnoreCase)) ? 4 : 1;
            }
        }

        return score;
    }

    private static IReadOnlySet<string> Tokenize(string text)
    {
        var normalized = text.ToLowerInvariant()
            .Replace(",", " ")
            .Replace(".", " ")
            .Replace("?", " ")
            .Replace("!", " ");

        var tokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 2)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var keyword in KeywordAliases(text))
        {
            tokens.Add(keyword);
        }

        return tokens;
    }

    private static IEnumerable<string> KeywordAliases(string text)
    {
        if (text.Contains("고전") || text.Contains("클래식") || text.Contains("오래") || text.Contains("aged", StringComparison.OrdinalIgnoreCase))
        {
            yield return "classic";
            yield return "aged";
        }

        if (text.Contains("현대") || text.Contains("깔끔") || text.Contains("modern", StringComparison.OrdinalIgnoreCase))
        {
            yield return "modern";
        }

        if (text.Contains("무대") || text.Contains("조명") || text.Contains("공연"))
        {
            yield return "stage";
        }
    }

    private static IReadOnlyList<ExhibitionKnowledgeDocument> BuiltInDocuments() =>
    [
        new()
        {
            Id = "artifact_classic_01",
            Title = "Classic Memory Frame",
            Summary = "오래된 무대의 기억을 담은 전시물",
            Description = "빛바랜 금속 프레임과 따뜻한 조명으로 과거 공연장의 분위기를 재해석한 작품입니다.",
            Tags = ["classic", "aged", "memory", "stage"],
            RecommendedEmotion = "happy",
            RecommendedAnimation = "explain",
            RecommendedStageEvent = "style_classic",
            GuideTone = "warm"
        },
        new()
        {
            Id = "artifact_modern_01",
            Title = "Modern Light Column",
            Summary = "간결한 선과 차가운 빛을 사용하는 현대적 전시물",
            Description = "수직적인 빛 기둥과 절제된 색으로 미래적인 전시 분위기를 만드는 오브젝트입니다.",
            Tags = ["modern", "clean", "light"],
            RecommendedEmotion = "surprise",
            RecommendedAnimation = "explain",
            RecommendedStageEvent = "style_modern",
            GuideTone = "clear"
        },
        new()
        {
            Id = "artifact_stage_01",
            Title = "Responsive Stage",
            Summary = "관람객 입력에 따라 조명과 캐릭터 반응이 바뀌는 무대",
            Description = "모바일 패널의 입력이 캐릭터 감정, 모션, 공간 스타일 변화로 이어지는 인터랙티브 무대입니다.",
            Tags = ["stage", "interaction", "character", "motion"],
            RecommendedEmotion = "happy",
            RecommendedAnimation = "wave",
            RecommendedStageEvent = "light.spotOn",
            GuideTone = "energetic"
        }
    ];
}
