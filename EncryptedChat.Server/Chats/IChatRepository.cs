namespace EncryptedChat.Server.Chats;

public interface IChatRepository
{
    Task<uint> SaveMessageAsync(ChatMessage message, CancellationToken token = default);

    Task<bool> DeleteMessageAsync(Guid senderId, Guid receiverId, uint messageId, CancellationToken token = default);

    Task<IEnumerable<ChatMessage>> GetMessagesAsync(
        Guid userId, Guid targetId, uint mimimumMessageId = 0, uint maximumMessageId = int.MaxValue, CancellationToken token = default);

    Task<IEnumerable<CryptographicKey>> GetCryptographicKeysAsync(
        Guid userId, Guid targetId, uint mimimumVersionId = 0, uint maximumVersionId = int.MaxValue, CancellationToken token = default);

    Task<uint> UpdateCryptographicKeysAsync(
        Guid userId, Guid targetId, ReadOnlyMemory<byte> ownEncryptedKey, uint ownVersion, ReadOnlyMemory<byte> targetEncryptedKey, uint targetVersion, CancellationToken token = default);
}
