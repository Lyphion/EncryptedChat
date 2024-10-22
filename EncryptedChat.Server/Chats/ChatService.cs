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

    private readonly IChatNotificationHandler _notificationHandler;

    public ChatService(ILogger<ChatService> logger, IChatRepository chatRepository, IChatNotificationHandler notificationHandler)
    {
        _logger = logger;
        _chatRepository = chatRepository;
        _notificationHandler = notificationHandler;
    }

    public override async Task<ChatMessageResponse> SendMessage(ChatMessageRequest request, ServerCallContext context)
    {
        string? senderIdString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(senderIdString, out var senderId))
            return new ChatMessageResponse { Success = false };

        if (!Guid.TryParse(request.TargetId, out var receiverId))
            return new ChatMessageResponse { Success = false };

        var now = DateTime.UtcNow;
        uint messageId = await _chatRepository.SaveMessageAsync(new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            EncryptedContentType = request.EncryptedContentType.ToByteArray(),
            EncryptedMessage = request.EncryptedMessage.ToByteArray(),
            Timestamp = now,
            KeyVersion = request.KeyVersion,
        }).ConfigureAwait(false);

        if (messageId == 0)
            return new ChatMessageResponse { Success = false };

        _logger.LogDebug("Sent message from '{SenderId}' to '{ReceiverId}'", senderId, receiverId);

        await _notificationHandler.PublishNotificationAsync(new ChatNotification
        {
            SenderId = senderId.ToString(),
            ReceiverId = receiverId.ToString(),
            MessageId = messageId,
            EncryptedContentType = request.EncryptedContentType,
            EncryptedMessage = request.EncryptedMessage,
            Timestamp = now.ToTimestamp(),
            KeyVersion = request.KeyVersion,
            Deleted = false
        }).ConfigureAwait(false);

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

        var messages = await _chatRepository.GetMessagesAsync(userId, targetId, request.MessageId, request.MessageId).ConfigureAwait(false);
        var message = messages.First();

        await _notificationHandler.PublishNotificationAsync(new ChatNotification
        {
            SenderId = userId.ToString(),
            ReceiverId = targetId.ToString(),
            MessageId = request.MessageId,
            EncryptedContentType = UnsafeByteOperations.UnsafeWrap(message.EncryptedContentType),
            EncryptedMessage = ByteString.Empty,
            Timestamp = message.Timestamp.ToTimestamp(),
            KeyVersion = message.KeyVersion,
            Deleted = true
        }).ConfigureAwait(false);

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
            EncryptedContentType = UnsafeByteOperations.UnsafeWrap(m.EncryptedContentType),
            EncryptedMessage = UnsafeByteOperations.UnsafeWrap(m.EncryptedMessage),
            Timestamp = m.Timestamp.ToTimestamp(),
            KeyVersion = m.KeyVersion,
            Deleted = m.Deleted
        }));

        return chat;
    }

    public override async Task<ChatOverviewResponse> GetChatOverview(ChatOverviewRequest request, ServerCallContext context)
    {
        string? idString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idString, out var id))
            return new ChatOverviewResponse();

        var messages = await _chatRepository
            .GetChatOverviewAsync(id)
            .ConfigureAwait(false);
        
        var chats = new ChatOverviewResponse();
        chats.Messages.AddRange(messages.Select(m => new ChatNotification
        {
            SenderId = m.SenderId.ToString(),
            ReceiverId = m.ReceiverId.ToString(),
            MessageId = m.MessageId,
            EncryptedContentType = UnsafeByteOperations.UnsafeWrap(m.EncryptedContentType),
            EncryptedMessage = UnsafeByteOperations.UnsafeWrap(m.EncryptedMessage),
            Timestamp = m.Timestamp.ToTimestamp(),
            KeyVersion = m.KeyVersion,
            Deleted = m.Deleted
        }));

        return chats;
    }

    public override async Task ReceiveMessages(ChatReceiveRequest request, IServerStreamWriter<ChatNotification> responseStream, ServerCallContext context)
    {
        string? idString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idString, out var id))
            return;

        var reader = _notificationHandler.Register(id);

        try
        {
            await foreach (var notification in reader.ReadAllAsync(context.CancellationToken).ConfigureAwait(false))
            {
                await responseStream.WriteAsync(notification).ConfigureAwait(false);
            }
        }
        finally
        {
            _notificationHandler.Unregister(id);
        }
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
            Key = UnsafeByteOperations.UnsafeWrap(k.EncryptedKey),
            Version = k.Version,
            PublicKeyVersion = k.PublicKeyVersion
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
            .UpdateCryptographicKeysAsync(userId, targetId, request.OwnEncryptedKey.Memory, request.OwnPublicKeyVersion, request.TargetEncryptedKey.Memory, request.TargetPublicKeyVersion)
            .ConfigureAwait(false);

        if (version > 0)
            _logger.LogInformation("Cryptographic Keys updated between '{UserId}' and '{TargetId}'", userId, targetId);

        return new CryptographicKeysUpdateResponse { Success = version > 0, Version = version };
    }
}
