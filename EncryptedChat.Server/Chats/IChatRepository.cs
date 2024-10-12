namespace EncryptedChat.Server.Chats;

public interface IChatRepository
{
    Task<uint> SaveMessageAsync(ChatMessage message, CancellationToken token = default);

    Task<bool> DeleteMessageAsync(Guid senderId, Guid receiverId, uint messageId, CancellationToken token = default);

    Task<IEnumerable<ChatMessage>> GetMessagesAsync(
        Guid userId, Guid targetId, uint mimimumMessageId = uint.MinValue, uint maximumMessageId = int.MaxValue, CancellationToken token = default);

    Task<IEnumerable<CryptographicKey>> GetCryptographicKeysAsync(
        Guid userId, Guid targetId, uint mimimumVersionId = uint.MinValue, uint maximumVersionId = int.MaxValue, CancellationToken token = default);

    Task<uint> UpdateCryptographicKeysAsync(
        Guid userId, Guid targetId, ReadOnlyMemory<byte> ownEncryptedKey, ReadOnlyMemory<byte> targetEncryptedKey, CancellationToken token = default);
}
