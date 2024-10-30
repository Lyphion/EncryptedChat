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
    ///     Collection of channels for the connected users.
    /// </summary>
    private readonly ConcurrentDictionary<string, (Guid clientId, ChannelWriter<ChatNotification> writer)[]> _notifications = new();

    /// <summary>
    ///     Global lock for editing the connected clients.
    /// </summary>
    private readonly SemaphoreSlim _semaphore = new(1);

    /// <inheritdoc />
    public async Task<ChannelReader<ChatNotification>> RegisterAsync(Guid clientId, Guid userId, CancellationToken token = default)
    {
        var channel = Channel.CreateUnbounded<ChatNotification>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        await _semaphore.WaitAsync(token).ConfigureAwait(false);

        try
        {
            // Add channel to user or create new reference
            _notifications.AddOrUpdate(
                userId.ToString(),
                _ => [(clientId, channel.Writer)],
                (_, channels) => [..channels, (userId, channel.Writer)]);
        }
        finally
        {
            _semaphore.Release();
        }

        return channel.Reader;
    }

    /// <inheritdoc />
    public async Task<bool> UnregisterAsync(Guid clientId, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);

        try
        {
            // Find user with the connected client id
            foreach (var (userId, clients) in _notifications)
            {
                for (int i = 0; i < clients.Length; i++)
                {
                    var (id, writer) = clients[i];
                    if (id != clientId)
                        continue;

                    writer.Complete();

                    // If this was the only client, remove from channels
                    if (clients.Length == 1)
                    {
                        _notifications.Remove(userId, out _);
                        return true;
                    }

                    // Remove client and keep others, copy arround index to new buffer
                    var channels = new (Guid, ChannelWriter<ChatNotification>)[clients.Length - 1];
                    Array.Copy(clients, 0, clients, 0, i);
                    Array.Copy(clients, i + 1, clients, i, clients.Length - i - 1);
                    _notifications[userId] = channels;
                    return true;
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return false;
    }

    /// <inheritdoc />
    public async Task PublishNotificationAsync(ChatNotification notification, CancellationToken token = default)
    {
        if (_notifications.TryGetValue(notification.SenderId, out var channels))
        {
            foreach (var (_, writer) in channels)
            {
                await writer.WriteAsync(notification, token).ConfigureAwait(false);
            }
        }

        if (_notifications.TryGetValue(notification.ReceiverId, out channels))
        {
            foreach (var (_, writer) in channels)
            {
                await writer.WriteAsync(notification, token).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var clients in _notifications.Values)
        foreach (var (_, writer) in clients)
            writer.Complete();

        _semaphore.Dispose();
    }
}
