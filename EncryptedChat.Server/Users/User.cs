namespace EncryptedChat.Server.Users;

/// <summary>
///     Registered user with properties.
/// </summary>
public sealed class User
{
    /// <summary>
    ///     Id of the user.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     Name of the user.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     Current public key of the user.
    /// </summary>
    public required byte[] PublicKey { get; set; }

    /// <summary>
    ///     Version of the public key.
    /// </summary>
    public required uint PublicKeyVersion { get; set; }
}
