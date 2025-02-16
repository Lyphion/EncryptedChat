﻿using System.Data.Common;
using Dapper;
using EncryptedChat.Server.Database;

namespace EncryptedChat.Server.Chats;

/// <summary>
///     Repository the save and receive chat message.
/// </summary>
public sealed class ChatRepository : IChatRepository
{
    /// <summary>
    ///     Logger for this repository.
    /// </summary>
    private readonly ILogger<ChatRepository> _logger;

    /// <summary>
    ///     Factory to create database connection.
    /// </summary>
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    ///     Create a new <see cref="ChatRepository"/> to manage users.
    /// </summary>
    /// <param name="logger">Logger for this repository.</param>
    /// <param name="connectionFactory">Factory to create database connection.</param>
    public ChatRepository(ILogger<ChatRepository> logger, IDbConnectionFactory connectionFactory)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<uint> SaveMessageAsync(ChatMessage message, CancellationToken token = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

            // Get next message id
            uint messageId = await connection.QueryFirstOrDefaultAsync<uint>(
                """
                SELECT coalesce(max(message_id), 0) FROM messages
                WHERE (sender_id = @SenderId AND receiver_id = @ReceiverId) OR (sender_id = @ReceiverId AND receiver_id = @SenderId)
                """,
                message
            ).ConfigureAwait(false) + 1;

            int result = await connection.ExecuteAsync(
                """
                PRAGMA foreign_keys = ON;
                INSERT INTO messages (sender_id, receiver_id, message_id, encrypted_content_type, encrypted_message, timestamp, key_version, deleted)
                VALUES (@SenderId, @ReceiverId, @MessageId, @EncryptedContentType, @EncryptedMessage, @Timestamp, @KeyVersion, @Deleted);
                """,
                new { message.SenderId, message.ReceiverId, MessageId = messageId, message.EncryptedContentType, message.EncryptedMessage, message.Created, message.KeyVersion, message.Deleted }
            ).ConfigureAwait(false);

            return result > 0 ? messageId : 0;
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Failed to save message between '{SenderId}' and '{ReceiverId}'", message.SenderId, message.ReceiverId);
            return 0;
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> EditMessageAsync(Guid senderId, Guid receiverId, uint messageId, ReadOnlyMemory<byte> encryptedMessage, uint keyVersion, DateTime timestamp, CancellationToken token = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

            int result = await connection.ExecuteAsync(
                """
                UPDATE messages
                SET encrypted_message = @encryptedMessage, key_version = @keyVersion, edited = @timestamp
                WHERE sender_id = @senderId AND receiver_id = @receiverId AND message_id = @messageId AND deleted = 0;
                """,
                new { senderId, receiverId, messageId, encryptedMessage, keyVersion, timestamp }
            ).ConfigureAwait(false);

            return result > 0;
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Failed to update message {MessageId} between '{SenderId}' and '{ReceiverId}'", messageId, senderId, receiverId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteMessageAsync(Guid senderId, Guid receiverId, uint messageId, DateTime timestamp, CancellationToken token = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

            int result = await connection.ExecuteAsync(
                """
                UPDATE messages
                SET deleted = 1, encrypted_message = '', edited = @timestamp
                WHERE sender_id = @senderId AND receiver_id = @receiverId AND message_id = @messageId AND deleted = 0;
                """,
                new { senderId, receiverId, messageId, timestamp }
            ).ConfigureAwait(false);

            return result > 0;
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Failed to delete message {MessageId} between '{SenderId}' and '{ReceiverId}'", messageId, senderId, receiverId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(
        Guid userId, Guid targetId, uint mimimumMessageId = uint.MinValue, uint maximumMessageId = int.MaxValue, CancellationToken token = default)
    {
        try
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
        catch (DbException ex)
        {
            _logger.LogError(ex, "Failed to get messages between '{UserId}' and '{TargetId}'", userId, targetId);
            return Array.Empty<ChatMessage>();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChatMessage>> GetChatOverviewAsync(Guid userId, CancellationToken token = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

            return await connection.QueryAsync<ChatMessage>(
                """
                SELECT a.* FROM messages a
                INNER JOIN (
                    SELECT sender_id, receiver_id, max(message_id) message_id
                    FROM messages
                    WHERE sender_id = @userId OR receiver_id = @userId
                    GROUP BY CASE WHEN sender_id = @userId THEN receiver_id ELSE sender_id END
                ) b ON a.sender_id = b.sender_id AND a.receiver_id = b.receiver_id AND a.message_id = b.message_id;
                """,
                new { userId }
            ).ConfigureAwait(false);
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Failed to get chats from '{UserId}'", userId);
            return Array.Empty<ChatMessage>();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CryptographicKey>> GetCryptographicKeysAsync(
        Guid userId, Guid targetId, uint mimimumVersionId = uint.MinValue, uint maximumVersionId = int.MaxValue, CancellationToken token = default)
    {
        try
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
        catch (DbException ex)
        {
            _logger.LogError(ex, "Failed to get keys between '{UserId}' and '{TargetId}'", userId, targetId);
            return Array.Empty<CryptographicKey>();
        }
    }

    /// <inheritdoc />
    public async Task<uint> UpdateCryptographicKeysAsync(Guid userId, Guid targetId, ReadOnlyMemory<byte> ownEncryptedKey, uint ownVersion, ReadOnlyMemory<byte> targetEncryptedKey, uint targetVersion, CancellationToken token = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

            // Get next version of the shared key
            uint version = await connection.QueryFirstOrDefaultAsync<uint>(
                """
                SELECT coalesce(max(version), 0) FROM keys
                WHERE user_id = @userId AND target_id = @targetId;
                """,
                new { userId, targetId }
            ).ConfigureAwait(false) + 1;

            int result = await connection.ExecuteAsync(
                """
                PRAGMA foreign_keys = ON;
                INSERT INTO keys (user_id, target_id, encrypted_key, version, public_key_version)
                VALUES (@userId, @targetId, @ownEncryptedKey, @version, @ownVersion), (@targetId, @userId, @targetEncryptedKey, @version, @targetVersion);
                """,
                new { userId, targetId, ownEncryptedKey, targetEncryptedKey, version, ownVersion, targetVersion }
            ).ConfigureAwait(false);

            return result > 1 ? version : 0;
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Failed to update cryptographic keys between '{UserId}' and '{TargetId}'", userId, targetId);
            return 0;
        }
    }
}
