using System.Collections.Concurrent;
using System.Threading.Channels;
using EncryptedChat.Common;

namespace EncryptedChat.Server.Users;

public sealed class UserUpdateNotificationHandler : IUserUpdateNotificationHandler, IDisposable
{
    private readonly ConcurrentDictionary<Guid, ChannelWriter<UserUpdateNotification>> _notifications = new();

    public ChannelReader<UserUpdateNotification> Register(Guid userId)
    {
        var channel = Channel.CreateUnbounded<UserUpdateNotification>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _notifications[userId] = channel.Writer;
        return channel.Reader;
    }

    public bool Unregister(Guid userId)
    {
        if (!_notifications.TryRemove(userId, out var writer))
            return false;

        writer.Complete();
        return true;
    }

    public async Task PublishNotificationAsync(UserUpdateNotification notification, CancellationToken token = default)
    {
        foreach (var writer in _notifications.Values)
        {
            await writer.WriteAsync(notification, token).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        foreach (var writer in _notifications.Values)
        {
            writer.Complete();
        }
    }
}
