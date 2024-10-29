namespace EncryptedChat.Server.Chats;

/// <summary>
///     Shared keys for that chat between two users.
/// </summary>
public sealed class CryptographicKey
{
    /// <summary>
    ///     Id of the user.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Id of the target.
    /// </summary>
    public required Guid TargetId { get; init; }

    /// <summary>
    ///     Encrypted shared key to chat with the target.
    /// </summary>
    public required byte[] EncryptedKey { get; init; }

    /// <summary>
    ///     Version of the shared key.
    /// </summary>
    public required uint Version { get; init; }

    /// <summary>
    ///     Version of the public key.
    /// </summary>
    public required uint PublicKeyVersion { get; init; }
}
