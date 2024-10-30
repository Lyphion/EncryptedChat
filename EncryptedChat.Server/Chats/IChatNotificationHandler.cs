using System.Threading.Channels;
using EncryptedChat.Common;

namespace EncryptedChat.Server.Chats;

/// <summary>
///     Handler to manage connected client for notications for chats.
/// </summary>
public interface IChatNotificationHandler
{
    /// <summary>
    ///     Register new channel for a client.
    /// </summary>
    /// <param name="clientId">Unique id of the client.</param>
    /// <param name="userId">Id of the user.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>Channel to receive notications.</returns>
    Task<ChannelReader<ChatNotification>> RegisterAsync(Guid clientId, Guid userId, CancellationToken token = default);

    /// <summary>
    ///     Unregister channel for a client.
    /// </summary>
    /// <param name="clientId">Unique id of the client.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns><c>true</c> if opteration was successful.</returns>
    Task<bool> UnregisterAsync(Guid clientId, CancellationToken token = default);

    /// <summary>
    ///     Publish a notification to connected clients.
    /// </summary>
    /// <param name="notification">Notication to public.</param>
    /// <param name="token">Token to cancel the operation.</param>
    Task PublishNotificationAsync(ChatNotification notification, CancellationToken token = default);
}
