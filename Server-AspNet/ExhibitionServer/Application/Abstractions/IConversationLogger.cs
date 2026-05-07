using ExhibitionServer.Application.Chat;

namespace ExhibitionServer.Application.Abstractions;

public interface IConversationLogger
{
    Task LogAsync(ChatLogEntry entry, CancellationToken cancellationToken = default);
}
