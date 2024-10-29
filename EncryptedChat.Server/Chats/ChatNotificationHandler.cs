using System.Collections.Concurrent;
using System.Threading.Channels;
using EncryptedChat.Common;

namespace EncryptedChat.Server.Chats;

/// <summary>
///     Handler to manage connected client for notications for chats.
/// </summary>
public sealed class ChatNotificationHandler : IChatNotificationHandler, IDisposable
{
    /// <summary>
    ///     Collection of channels for the connected clients.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, (Guid, ChannelWriter<ChatNotification>)> _notifications = new();

    /// <inheritdoc />
    public ChannelReader<ChatNotification> Register(Guid clientId, Guid userId)
    {
        var channel = Channel.CreateUnbounded<ChatNotification>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _notifications[clientId] = (userId, channel.Writer);
        return channel.Reader;
    }

    /// <inheritdoc />
    public bool Unregister(Guid clientId)
    {
        if (!_notifications.TryRemove(clientId, out var writer))
            return false;

        writer.Item2.Complete();
        return true;
    }

    /// <inheritdoc />
    public async Task PublishNotificationAsync(ChatNotification notification, CancellationToken token = default)
    {
        foreach (var (id, writer) in _notifications.Values)
        {
            string targetId = id.ToString();
            if (targetId.Equals(notification.SenderId) || targetId.Equals(notification.ReceiverId))
                await writer.WriteAsync(notification, token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var (_, writer) in _notifications.Values)
        {
            writer.Complete();
        }
    }
}
