namespace EncryptedChat.Client.Services;

public interface ITokenProvider
{
    Task<string> GetTokenAsync(CancellationToken token = default);
}
