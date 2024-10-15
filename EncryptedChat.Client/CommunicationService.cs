using EncryptedChat.Common;
using Google.Protobuf;
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
        var response = await _chatClient.SendMessageAsync(new ChatMessageRequest
        {
            TargetId = Guid.NewGuid().ToString(),
            EncryptedMessage = ByteString.Empty,
            KeyVersion = 1
        }, cancellationToken: token).ConfigureAwait(false);
    }
}
