namespace EncryptedChat.Server.Chats;

public sealed class ChatMessage
{
    public required Guid SenderId { get; init; }

    public required Guid ReceiverId { get; init; }

    public uint MessageId { get; init; }

    public required byte[] EncryptedMessage { get; set; }

    public required DateTime Timestamp { get; init; }

    public required uint KeyVersion { get; init; }

    public bool Deleted { get; set; }
}
