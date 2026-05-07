namespace ExhibitionServer.Application.Abstractions;

public interface IEmbeddingClient
{
    bool IsConfigured { get; }

    Task<float[]?> CreateEmbeddingAsync(string input, CancellationToken cancellationToken = default);
}
