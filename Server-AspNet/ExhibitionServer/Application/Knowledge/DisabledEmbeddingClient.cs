using ExhibitionServer.Application.Abstractions;

namespace ExhibitionServer.Application.Knowledge;

public sealed class DisabledEmbeddingClient : IEmbeddingClient
{
    public bool IsConfigured => false;

    public Task<float[]?> CreateEmbeddingAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<float[]?>(null);
    }
}
