using System.Threading.Channels;
using EncryptedChat.Common;

namespace EncryptedChat.Server.Users;

/// <summary>
///     Handler to manage connected client for updates on user properties.
/// </summary>
public interface IUserUpdateNotificationHandler
{
    /// <summary>
    ///     Register new channel for a client.
    /// </summary>
    /// <param name="clientId">Unique id of the client.</param>
    /// <param name="userId">Id of the user.</param>
    /// <returns>Channel to receive notications.</returns>
    ChannelReader<UserUpdateNotification> Register(Guid clientId, Guid userId);

    /// <summary>
    ///     Unregister channel for a client.
    /// </summary>
    /// <param name="clientId">Unique id of the client.</param>
    /// <returns><c>true</c> if opteration was successful.</returns>
    bool Unregister(Guid clientId);

    /// <summary>
    ///     Publish a notification to connected clients.
    /// </summary>
    /// <param name="notification">Notication to public.</param>
    /// <param name="token">Token to cancel the operation.</param>
    Task PublishNotificationAsync(UserUpdateNotification notification, CancellationToken token = default);
}
