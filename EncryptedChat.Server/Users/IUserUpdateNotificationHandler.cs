using System.Threading.Channels;
using EncryptedChat.Common;

namespace EncryptedChat.Server.Users;

public interface IUserUpdateNotificationHandler
{
    ChannelReader<UserUpdateNotification> Register(Guid userId);

    bool Unregister(Guid userId);

    Task PublishNotificationAsync(UserUpdateNotification notification, CancellationToken token = default);
}
