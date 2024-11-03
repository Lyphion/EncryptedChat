namespace EncryptedChat.Server.Chats;

/// <summary>
///     Repository the save and receive chat message.
/// </summary>
public interface IChatRepository
{
    /// <summary>
    ///     Save a new message between two users.
    /// </summary>
    /// <param name="message">Message to save.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>Id of the message in the chat or <c>0</c> if the operation failed.</returns>
    Task<uint> SaveMessageAsync(ChatMessage message, CancellationToken token = default);
    
    /// <summary>
    ///     Edit a message between users.
    /// </summary>
    /// <param name="senderId">Id of the sender.</param>
    /// <param name="receiverId">Id of the receiver.</param>
    /// <param name="messageId">Id the message in the chat.</param>
    /// <param name="encryptedMessage">New encrypted message.</param>
    /// <param name="keyVersion">The version of the shared encryption key used.</param>
    /// <param name="timestamp">Timestamp when the message was edited.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns><c>true</c> if operation was successful.</returns>
    Task<bool> EditMessageAsync(Guid senderId, Guid receiverId, uint messageId, ReadOnlyMemory<byte> encryptedMessage, uint keyVersion, DateTime timestamp, CancellationToken token = default);

    /// <summary>
    ///     Delete a message in the chat between two users.
    /// </summary>
    /// <param name="senderId">Id of the sender.</param>
    /// <param name="receiverId">Id of the receiver.</param>
    /// <param name="messageId">Id the message in the chat.</param>
    /// <param name="timestamp">Timestamp when the message was deleted.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns><c>true</c> if operation was successful.</returns>
    Task<bool> DeleteMessageAsync(Guid senderId, Guid receiverId, uint messageId, DateTime timestamp, CancellationToken token = default);

    /// <summary>
    ///     Receive messages in the chat between two users.
    /// </summary>
    /// <param name="userId">Id of the first user of the chat.</param>
    /// <param name="targetId">Id of the second user of the chat.</param>
    /// <param name="mimimumMessageId">Id of the earliest message.</param>
    /// <param name="maximumMessageId">Id of the latest message.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>Collection of messages between the users.</returns>
    Task<IEnumerable<ChatMessage>> GetMessagesAsync(
        Guid userId, Guid targetId, uint mimimumMessageId = 0, uint maximumMessageId = int.MaxValue, CancellationToken token = default);
    
    /// <summary>
    ///     Receive a overview of all active chats of a user.
    /// </summary>
    /// <param name="userId">Id of the user.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>Collection of the latest messages of all active chats of the user.</returns>
    Task<IEnumerable<ChatMessage>> GetChatOverviewAsync(
        Guid userId, CancellationToken token = default);

    /// <summary>
    ///     Receive the encrypted shared keys between two users. 
    /// </summary>
    /// <param name="userId">Id of the user.</param>
    /// <param name="targetId">Id of the target user.</param>
    /// <param name="mimimumVersionId">Id of the earliest key.</param>
    /// <param name="maximumVersionId">Id of the latest key.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>Collection of encrypted shared keys for the user.</returns>
    Task<IEnumerable<CryptographicKey>> GetCryptographicKeysAsync(
        Guid userId, Guid targetId, uint mimimumVersionId = 0, uint maximumVersionId = int.MaxValue, CancellationToken token = default);

    /// <summary>
    ///     Update the encrypted shared keys between two users. 
    /// </summary>
    /// <param name="userId">Id of the user.</param>
    /// <param name="targetId">Id of the target user.</param>
    /// <param name="ownEncryptedKey">Encrypted key of the user.</param>
    /// <param name="ownVersion">Version of the public key of the user.</param>
    /// <param name="targetEncryptedKey">Encrypted key of the target.</param>
    /// <param name="targetVersion">Version of the public key of the target.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>Version of the key, or <c>0</c> if the operation failed.</returns>
    Task<uint> UpdateCryptographicKeysAsync(
        Guid userId, Guid targetId, ReadOnlyMemory<byte> ownEncryptedKey, uint ownVersion, ReadOnlyMemory<byte> targetEncryptedKey, uint targetVersion, CancellationToken token = default);
}
