using System.Collections.Concurrent;
using System.Threading.Channels;
using EncryptedChat.Common;

namespace EncryptedChat.Server.Users;

/// <summary>
///     Handler to manage connected client for updates on user properties.
/// </summary>
public sealed class UserUpdateNotificationHandler : IUserUpdateNotificationHandler, IDisposable
{
    /// <summary>
    ///     Collection of channels for the connected clients.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, ChannelWriter<UserUpdateNotification>> _notifications = new();

    /// <inheritdoc />
    public ChannelReader<UserUpdateNotification> Register(Guid clientId, Guid userId)
    {
        var channel = Channel.CreateUnbounded<UserUpdateNotification>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _notifications[clientId] = channel.Writer;
        return channel.Reader;
    }

    /// <inheritdoc />
    public bool Unregister(Guid clientId)
    {
        if (!_notifications.TryRemove(clientId, out var writer))
            return false;

        writer.Complete();
        return true;
    }

    /// <inheritdoc />
    public async Task PublishNotificationAsync(UserUpdateNotification notification, CancellationToken token = default)
    {
        foreach (var writer in _notifications.Values)
        {
            await writer.WriteAsync(notification, token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var writer in _notifications.Values)
        {
            writer.Complete();
        }
    }
}
