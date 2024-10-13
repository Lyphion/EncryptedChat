using System.Collections.Concurrent;
using System.Threading.Channels;
using EncryptedChat.Common;

namespace EncryptedChat.Server.Chats;

public sealed class ChatNotificationHandler : IChatNotificationHandler, IDisposable
{
    private readonly ConcurrentDictionary<string, ChannelWriter<ChatNotification>> _notifications = new();

    public ChannelReader<ChatNotification> Register(Guid userId)
    {
        var channel = Channel.CreateUnbounded<ChatNotification>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _notifications[userId.ToString()] = channel.Writer;
        return channel.Reader;
    }

    public bool Unregister(Guid userId)
    {
        if (!_notifications.TryRemove(userId.ToString(), out var writer))
            return false;

        writer.Complete();
        return true;
    }

    public async Task PublishNotificationAsync(ChatNotification notification, CancellationToken token = default)
    {
        if (_notifications.TryGetValue(notification.SenderId, out var writer))
            await writer.WriteAsync(notification, token).ConfigureAwait(false);
        if (_notifications.TryGetValue(notification.ReceiverId, out writer))
            await writer.WriteAsync(notification, token).ConfigureAwait(false);
    }

    public void Dispose()
    {
        foreach (var writer in _notifications.Values)
        {
            writer.Complete();
        }
    }
}
