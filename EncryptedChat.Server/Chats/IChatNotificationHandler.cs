using System.Threading.Channels;
using EncryptedChat.Common;

namespace EncryptedChat.Server.Chats;

public interface IChatNotificationHandler
{
    ChannelReader<ChatNotification> Register(Guid userId);

    bool Unregister(Guid userId);

    Task PublishNotificationAsync(ChatNotification notification, CancellationToken token = default);
}
