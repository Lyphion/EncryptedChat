namespace EncryptedChat.Server.Chats;

public sealed class CryptographicKey
{
    public required Guid UserId { get; init; }

    public required Guid TargetId { get; init; }

    public required byte[] EncryptedKey { get; init; }

    public required uint Version { get; init; }
}
