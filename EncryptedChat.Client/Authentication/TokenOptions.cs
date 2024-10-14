namespace EncryptedChat.Client.Authentication;

public sealed class TokenOptions
{
    public required string Issuer { get; init; }

    public required string ClientId { get; init; }

    public required string UserName { get; init; }

    public required string Password { get; init; }

    public required string Scope { get; init; }
}
