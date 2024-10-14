using EncryptedChat.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EncryptedChat.Client;

public sealed class CommunicationService : BackgroundService
{
    private readonly ILogger<CommunicationService> _logger;

    private readonly Chat.ChatClient _chatClient;
    private readonly User.UserClient _userClient;

    public CommunicationService(ILogger<CommunicationService> logger, Chat.ChatClient chatClient, User.UserClient userClient)
    {
        _logger = logger;
        _chatClient = chatClient;
        _userClient = userClient;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        var users = await _userClient.GetUsersAsync(new UsersRequest(), cancellationToken: token).ConfigureAwait(false);
        
    }
}
