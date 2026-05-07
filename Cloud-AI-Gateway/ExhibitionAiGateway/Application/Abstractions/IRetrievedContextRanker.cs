using Exhibition.Shared.Ai;

namespace ExhibitionAiGateway.Application.Abstractions;

public interface IRetrievedContextRanker
{
    Task<IReadOnlyList<AiRetrievedContextDto>> RankAsync(
        string query,
        IReadOnlyList<AiRetrievedContextDto> contexts,
        CancellationToken cancellationToken = default);
}
