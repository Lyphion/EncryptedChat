using Dapper;
using EncryptedChat.Server.Database;

namespace EncryptedChat.Server.Chats;

public sealed class ChatRepository : IChatRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChatRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<uint> SaveMessageAsync(ChatMessage message, CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        uint messageId = await connection.QueryFirstOrDefaultAsync<uint>(
            """
            SELECT coalesce(max(message_id), 0) FROM messages
            WHERE (sender_id = @SenderId AND receiver_id = @ReceiverId) OR (sender_id = @ReceiverId AND receiver_id = @SenderId)
            """,
            message
        ).ConfigureAwait(false) + 1;

        int result = await connection.ExecuteAsync(
            """
            INSERT INTO messages (sender_id, receiver_id, message_id, encrypted_message, timestamp, key_version, deleted)
            VALUES (@SenderId, @ReceiverId, @MessageId, @EncryptedMessage, @Timestamp, @KeyVersion, @Deleted);
            """,
            new { message.SenderId, message.ReceiverId, MessageId = messageId, message.EncryptedMessage, message.Timestamp, message.KeyVersion, message.Deleted }
        ).ConfigureAwait(false);

        return result > 0 ? messageId : 0;
    }

    public async Task<bool> DeleteMessageAsync(Guid senderId, Guid receiverId, uint messageId, CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        int result = await connection.ExecuteAsync(
            """
            UPDATE messages
            SET deleted = 1, encrypted_message = ''
            WHERE sender_id = @senderId AND receiver_id = @receiverId AND message_id = @messageId;
            """,
            new { senderId, receiverId, messageId }
        ).ConfigureAwait(false);

        return result > 0;
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(
        Guid userId, Guid targetId, uint mimimumMessageId = uint.MinValue, uint maximumMessageId = int.MaxValue, CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        return await connection.QueryAsync<ChatMessage>(
            """
            SELECT * FROM messages
            WHERE ((sender_id = @userId AND receiver_id = @targetId) OR (sender_id = @targetId AND receiver_id = @userId))
                AND message_id BETWEEN @mimimumMessageId AND @maximumMessageId;
            """,
            new { userId, targetId, mimimumMessageId, maximumMessageId }
        ).ConfigureAwait(false);
    }

    public async Task<IEnumerable<CryptographicKey>> GetCryptographicKeysAsync(
        Guid userId, Guid targetId, uint mimimumVersionId = uint.MinValue, uint maximumVersionId = int.MaxValue, CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        return await connection.QueryAsync<CryptographicKey>(
            """
            SELECT * FROM keys
            WHERE user_id = @userId AND target_id = @targetId AND version BETWEEN @mimimumVersionId AND @maximumVersionId;
            """,
            new { userId, targetId, mimimumVersionId, maximumVersionId }
        ).ConfigureAwait(false);
    }

    public async Task<uint> UpdateCryptographicKeysAsync(
        Guid userId, Guid targetId, ReadOnlyMemory<byte> ownEncryptedKey, ReadOnlyMemory<byte> targetEncryptedKey, CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        uint version = await connection.QueryFirstOrDefaultAsync<uint>(
            """
            SELECT coalesce(max(version), 0) FROM keys
            WHERE user_id = @userId AND target_id = @targetId;
            """,
            new { userId, targetId }
        ).ConfigureAwait(false) + 1;

        int result = await connection.ExecuteAsync(
            """
            INSERT INTO keys (user_id, target_id, encrypted_key, version)
            VALUES (@userId, @targetId, @ownEncryptedKey, @version), (@targetId, @userId, @targetEncryptedKey, @version);
            """,
            new { userId, targetId, ownEncryptedKey, targetEncryptedKey, version }
        ).ConfigureAwait(false);

        return result > 1 ? version : 0;
    }
}
