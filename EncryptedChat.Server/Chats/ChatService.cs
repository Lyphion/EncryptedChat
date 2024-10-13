using System.Security.Claims;
using EncryptedChat.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace EncryptedChat.Server.Chats;

[Authorize]
public sealed class ChatService : Chat.ChatBase
{
    private readonly ILogger<ChatService> _logger;

    private readonly IChatRepository _chatRepository;

    public ChatService(ILogger<ChatService> logger, IChatRepository chatRepository)
    {
        _logger = logger;
        _chatRepository = chatRepository;
    }

    public override async Task<ChatMessageResponse> SendMessage(ChatMessageRequest request, ServerCallContext context)
    {
        string? senderIdString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(senderIdString, out var senderId))
            return new ChatMessageResponse { Success = false };

        if (!Guid.TryParse(request.TargetId, out var receiverId))
            return new ChatMessageResponse { Success = false };

        uint messageId = await _chatRepository.SaveMessageAsync(new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            EncryptedMessage = request.EncryptedMessage.ToByteArray(),
            Timestamp = DateTime.UtcNow,
            KeyVersion = request.KeyVersion,
        }).ConfigureAwait(false);

        if (messageId == 0)
            return new ChatMessageResponse { Success = false };

        _logger.LogDebug("Sent message from '{SenderId}' to '{ReceiverId}'", senderId, receiverId);

        // TODO Send to target if connected

        return new ChatMessageResponse { Success = true };
    }

    public override async Task<DeleteChatMessageResponse> DeleteMessage(DeleteChatMessageRequest request, ServerCallContext context)
    {
        string? userIdString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return new DeleteChatMessageResponse { Success = false };

        if (!Guid.TryParse(request.TargetId, out var targetId))
            return new DeleteChatMessageResponse { Success = false };

        bool success = await _chatRepository
            .DeleteMessageAsync(userId, targetId, request.MessageId)
            .ConfigureAwait(false);

        if (!success) 
            return new DeleteChatMessageResponse { Success = false };
        
        _logger.LogDebug("Deleted message {MessageId} between '{UserId}' and '{TargetId}'", request.MessageId, userId, targetId);

        // TODO Send to target if connected

        return new DeleteChatMessageResponse { Success = true };
    }

    public override async Task<ChatResponse> GetMessages(ChatRequest request, ServerCallContext context)
    {
        string? senderIdString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(senderIdString, out var senderId))
            return new ChatResponse();

        if (!Guid.TryParse(request.TargetId, out var receiverId))
            return new ChatResponse();

        uint max = request.HasMaximumMessageId ? request.MaximumMessageId : int.MaxValue;

        var messages = await _chatRepository
            .GetMessagesAsync(senderId, receiverId, request.MinimumMessageId, max)
            .ConfigureAwait(false);

        var chat = new ChatResponse();
        chat.Messages.AddRange(messages.Select(m => new ChatNotification
        {
            SenderId = m.SenderId.ToString(),
            ReceiverId = m.ReceiverId.ToString(),
            MessageId = m.MessageId,
            EncryptedMessage = ByteString.CopyFrom(m.EncryptedMessage),
            Timestamp = m.Timestamp.ToTimestamp(),
            KeyVersion = m.KeyVersion,
            Deleted = m.Deleted
        }));

        return chat;
    }

    public override async Task ReceiveMessages(ChatReceiveRequest request, IServerStreamWriter<ChatNotification> responseStream, ServerCallContext context)
    {
        // TODO Send messages
    }

    public override async Task<CryptographicKeysReponse> GetCryptographicKeys(CryptographicKeysRequest request, ServerCallContext context)
    {
        string? userIdString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return new CryptographicKeysReponse();

        if (!Guid.TryParse(request.TargetId, out var targetId))
            return new CryptographicKeysReponse();

        uint max = request.HasMaximumVersion ? request.MaximumVersion : int.MaxValue;

        var keys = await _chatRepository
            .GetCryptographicKeysAsync(userId, targetId, request.MinimumVersion, max)
            .ConfigureAwait(false);

        var response = new CryptographicKeysReponse();
        response.Keys.AddRange(keys.Select(k => new CryptographicKeysReponse.Types.CryptographicKey
        {
            Key = ByteString.CopyFrom(k.EncryptedKey),
            Version = k.Version
        }));

        return response;
    }

    public override async Task<CryptographicKeysUpdateResponse> UpdateCryptographicKeys(CryptographicKeysUpdateRequest request, ServerCallContext context)
    {
        string? userIdString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return new CryptographicKeysUpdateResponse { Success = false };

        if (!Guid.TryParse(request.TargetId, out var targetId))
            return new CryptographicKeysUpdateResponse { Success = false };

        uint version = await _chatRepository
            .UpdateCryptographicKeysAsync(userId, targetId, request.OwnEncryptedKey.Memory, request.TargetEncryptedKey.Memory)
            .ConfigureAwait(false);

        if (version > 0)
            _logger.LogInformation("Cryptographic Keys updated between '{UserId}' and '{TargetId}'", userId, targetId);

        return new CryptographicKeysUpdateResponse { Success = version > 0, Version = version };
    }
}
