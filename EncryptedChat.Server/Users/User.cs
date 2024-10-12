namespace EncryptedChat.Server.Users;

public sealed class User
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public required byte[] PublicKey { get; set; }
}
