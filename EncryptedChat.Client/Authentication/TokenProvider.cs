using EncryptedChat.Client.Services;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace EncryptedChat.Client.Authentication;

public sealed class TokenProvider : ITokenProvider
{
    public const string ClientName = "Auth";

    private readonly IOptions<TokenOptions> _options;
    private readonly IHttpClientFactory _clientFactory;

    private readonly DiscoveryCache _discoveryCache;

    public TokenProvider(IOptions<TokenOptions> options, IHttpClientFactory clientFactory)
    {
        _options = options;
        _clientFactory = clientFactory;

        _discoveryCache = new DiscoveryCache(
            _options.Value.Issuer,
            () => _clientFactory.CreateClient(ClientName),
            new DiscoveryPolicy
            {
#if DEBUG
                RequireHttps = false
#endif
            }
        );
    }

    public async Task<string> GetTokenAsync(CancellationToken token = default)
    {
        var discovery = await _discoveryCache.GetAsync().ConfigureAwait(false);

        // Check if response is valid
        if (discovery.IsError)
            throw new Exception($"Failed to get token: {discovery.Error}");

        using var client = _clientFactory.CreateClient(ClientName);

        // Receive authentication token
        var tokenResponse = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
        {
            Address = discovery.TokenEndpoint,
            ClientId = _options.Value.ClientId,
            UserName = _options.Value.UserName,
            Password = _options.Value.Password,
            Scope = _options.Value.Scope
        }, token).ConfigureAwait(false);

        // Check if token is valid
        if (tokenResponse.IsError)
            throw new Exception($"Failed to get token: {discovery.Error}");

        return tokenResponse.AccessToken!;
    }
}
