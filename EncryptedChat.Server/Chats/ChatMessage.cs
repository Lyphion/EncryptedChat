namespace EncryptedChat.Server.Chats;

/// <summary>
///     Message of a chat between two users.
/// </summary>
public sealed class ChatMessage
{
    /// <summary>
    ///     Id of the sender.
    /// </summary>
    public required Guid SenderId { get; init; }

    /// <summary>
    ///     Id of receiver.
    /// </summary>
    public required Guid ReceiverId { get; init; }

    /// <summary>
    ///     Id/Index the message in the chat between the users.
    /// </summary>
    public uint MessageId { get; init; }

    /// <summary>
    ///     Encrypted type of the messge.
    /// </summary>
    public required byte[] EncryptedContentType { get; init; }
    
    /// <summary>
    ///     Encrypted message from the sender for the receiver.
    /// </summary>
    /// <remarks>
    ///     If <see cref="Deleted"/> is <c>true</c> than this data is invalid or empty.
    /// </remarks>
    public required byte[] EncryptedMessage { get; set; }

    /// <summary>
    ///     Timestamp when the message was sent.
    /// </summary>
    public required DateTime Created { get; init; }

    /// <summary>
    ///     Optional timestamp when the message was edited or deleted.
    /// </summary>
    public DateTime? Edited { get; set; }

    /// <summary>
    ///     The version of the shared encryption key used.
    /// </summary>
    public required uint KeyVersion { get; init; }

    /// <summary>
    ///     Wether the message was deleted.
    /// </summary>
    public bool Deleted { get; set; }
}
