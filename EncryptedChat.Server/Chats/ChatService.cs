using System.Security.Claims;
using EncryptedChat.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace EncryptedChat.Server.Chats;

/// <summary>
///     Service to handle gRPC requests for chat operations.
/// </summary>
[Authorize]
public sealed class ChatService : Chat.ChatBase
{
    /// <summary>
    ///     Logger for this service.
    /// </summary>
    private readonly ILogger<ChatService> _logger;

    /// <summary>
    ///     Repository for managing chats.
    /// </summary>
    private readonly IChatRepository _chatRepository;

    /// <summary>
    ///     Handler to manage communication between clients.
    /// </summary>
    private readonly IChatNotificationHandler _notificationHandler;

    /// <summary>
    ///     Create a new <see cref="ChatService"/> to handle gRPC requests.
    /// </summary>
    /// <param name="logger">Logger for this service.</param>
    /// <param name="chatRepository">Repository for managing chats.</param>
    /// <param name="notificationHandler">Handler to manage communication between clients.</param>
    public ChatService(ILogger<ChatService> logger, IChatRepository chatRepository, IChatNotificationHandler notificationHandler)
    {
        _logger = logger;
        _chatRepository = chatRepository;
        _notificationHandler = notificationHandler;
    }

    /// <summary>
    ///     Send a message to another user.
    /// </summary>
    /// <param name="request">Request parameter.</param>
    /// <param name="context">Connection context.</param>
    /// <returns>Success or failure message.</returns>
    public override async Task<ChatMessageResponse> SendMessage(ChatMessageRequest request, ServerCallContext context)
    {
        // Reveive id of the user
        string? senderIdString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(senderIdString, out var senderId))
            return new ChatMessageResponse { Success = false };

        // Check if a valid target id is provied
        if (!Guid.TryParse(request.TargetId, out var receiverId))
            return new ChatMessageResponse { Success = false };

        // Save message
        var now = DateTime.UtcNow;
        uint messageId = await _chatRepository.SaveMessageAsync(new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            EncryptedContentType = request.EncryptedContentType.ToByteArray(),
            EncryptedMessage = request.EncryptedMessage.ToByteArray(),
            Created = now,
            KeyVersion = request.KeyVersion,
        }).ConfigureAwait(false);

        // Check if saving was succesful
        if (messageId == 0)
            return new ChatMessageResponse { Success = false };

        _logger.LogDebug("Sent message from '{SenderId}' to '{ReceiverId}'", senderId, receiverId);

        // Notify connected clients in the background
        _ = _notificationHandler.PublishNotificationAsync(new ChatNotification
        {
            SenderId = senderId.ToString(),
            ReceiverId = receiverId.ToString(),
            MessageId = messageId,
            EncryptedContentType = request.EncryptedContentType,
            EncryptedMessage = request.EncryptedMessage,
            Created = now.ToTimestamp(),
            Edited = null,
            KeyVersion = request.KeyVersion,
            Deleted = false
        }).ConfigureAwait(false);

        return new ChatMessageResponse { Success = true };
    }

    /// <summary>
    ///     Edit a message in the chat between two users.
    /// </summary>
    /// <param name="request">Request parameter.</param>
    /// <param name="context">Connection context.</param>
    /// <returns>Success or failure message.</returns>
    public override async Task<EditChatMessageResponse> EditMessage(EditChatMessageRequest request, ServerCallContext context)
    {
        // Reveive id of the user
        string? userIdString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return new EditChatMessageResponse { Success = false };

        // Check if a valid target id is provied
        if (!Guid.TryParse(request.TargetId, out var targetId))
            return new EditChatMessageResponse { Success = false };

        var now = DateTime.UtcNow;
        bool success = await _chatRepository
            .EditMessageAsync(userId, targetId, request.MessageId, request.EncryptedMessage.Memory, request.KeyVersion, now)
            .ConfigureAwait(false);

        // Check if edit was succesful
        if (!success)
            return new EditChatMessageResponse { Success = false };

        _logger.LogDebug("Edited message {MessageId} between '{UserId}' and '{TargetId}'", request.MessageId, userId, targetId);

        // Get edited message to send update
        var messages = await _chatRepository.GetMessagesAsync(userId, targetId, request.MessageId, request.MessageId).ConfigureAwait(false);
        var message = messages.First();

        // Notify connected clients in the background
        _ = _notificationHandler.PublishNotificationAsync(new ChatNotification
        {
            SenderId = message.SenderId.ToString(),
            ReceiverId = message.ReceiverId.ToString(),
            MessageId = message.MessageId,
            EncryptedContentType = UnsafeByteOperations.UnsafeWrap(message.EncryptedContentType),
            EncryptedMessage = UnsafeByteOperations.UnsafeWrap(message.EncryptedMessage),
            Created = message.Created.ToTimestamp(),
            Edited = message.Edited?.ToTimestamp(),
            KeyVersion = message.KeyVersion,
            Deleted = message.Deleted
        }).ConfigureAwait(false);

        return new EditChatMessageResponse { Success = true };
    }

    /// <summary>
    ///     Delete a message in the chat between two users.
    /// </summary>
    /// <param name="request">Request parameter.</param>
    /// <param name="context">Connection context.</param>
    /// <returns>Success or failure message.</returns>
    public override async Task<DeleteChatMessageResponse> DeleteMessage(DeleteChatMessageRequest request, ServerCallContext context)
    {
        // Reveive id of the user
        string? userIdString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return new DeleteChatMessageResponse { Success = false };

        // Check if a valid target id is provied
        if (!Guid.TryParse(request.TargetId, out var targetId))
            return new DeleteChatMessageResponse { Success = false };

        var now = DateTime.UtcNow;
        bool success = await _chatRepository
            .DeleteMessageAsync(userId, targetId, request.MessageId, now)
            .ConfigureAwait(false);

        // Check if deletion was succesful
        if (!success)
            return new DeleteChatMessageResponse { Success = false };

        _logger.LogDebug("Deleted message {MessageId} between '{UserId}' and '{TargetId}'", request.MessageId, userId, targetId);

        // Get deleted message to send update
        var messages = await _chatRepository.GetMessagesAsync(userId, targetId, request.MessageId, request.MessageId).ConfigureAwait(false);
        var message = messages.First();

        // Notify connected clients in the background
        _ = _notificationHandler.PublishNotificationAsync(new ChatNotification
        {
            SenderId = message.SenderId.ToString(),
            ReceiverId = message.ReceiverId.ToString(),
            MessageId = message.MessageId,
            EncryptedContentType = UnsafeByteOperations.UnsafeWrap(message.EncryptedContentType),
            EncryptedMessage = UnsafeByteOperations.UnsafeWrap(message.EncryptedMessage),
            Created = message.Created.ToTimestamp(),
            Edited = message.Edited?.ToTimestamp(),
            KeyVersion = message.KeyVersion,
            Deleted = message.Deleted
        }).ConfigureAwait(false);

        return new DeleteChatMessageResponse { Success = true };
    }

    /// <summary>
    ///     Receive message of a chat between two users.
    /// </summary>
    /// <param name="request">Request parameter.</param>
    /// <param name="context">Connection context.</param>
    /// <returns>Collection of message of the chat.</returns>
    public override async Task<ChatResponse> GetMessages(ChatRequest request, ServerCallContext context)
    {
        // Reveive id of the user
        string? senderIdString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(senderIdString, out var senderId))
            return new ChatResponse();

        // Check if a valid target id is provied
        if (!Guid.TryParse(request.TargetId, out var receiverId))
            return new ChatResponse();

        uint max = request.HasMaximumMessageId ? request.MaximumMessageId : int.MaxValue;

        // Get messages
        var messages = await _chatRepository
            .GetMessagesAsync(senderId, receiverId, request.MinimumMessageId, max)
            .ConfigureAwait(false);

        // Convert to communication data structure
        var chat = new ChatResponse();
        chat.Messages.AddRange(messages.Select(m => new ChatNotification
        {
            SenderId = m.SenderId.ToString(),
            ReceiverId = m.ReceiverId.ToString(),
            MessageId = m.MessageId,
            EncryptedContentType = UnsafeByteOperations.UnsafeWrap(m.EncryptedContentType),
            EncryptedMessage = UnsafeByteOperations.UnsafeWrap(m.EncryptedMessage),
            Created = m.Created.ToTimestamp(),
            Edited = m.Edited?.ToTimestamp(),
            KeyVersion = m.KeyVersion,
            Deleted = m.Deleted
        }));

        return chat;
    }

    /// <summary>
    ///     Receive an overview of all active chats of the user.
    /// </summary>
    /// <param name="request">Request parameter.</param>
    /// <param name="context">Connection context.</param>
    /// <returns>Overview with the last messages of the chats.</returns>
    public override async Task<ChatOverviewResponse> GetChatOverview(ChatOverviewRequest request, ServerCallContext context)
    {
        // Reveive id of the user
        string? idString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idString, out var id))
            return new ChatOverviewResponse();

        // Get last messages
        var messages = await _chatRepository
            .GetChatOverviewAsync(id)
            .ConfigureAwait(false);

        // Convert to communication data structure
        var chats = new ChatOverviewResponse();
        chats.Messages.AddRange(messages.Select(m => new ChatNotification
        {
            SenderId = m.SenderId.ToString(),
            ReceiverId = m.ReceiverId.ToString(),
            MessageId = m.MessageId,
            EncryptedContentType = UnsafeByteOperations.UnsafeWrap(m.EncryptedContentType),
            EncryptedMessage = UnsafeByteOperations.UnsafeWrap(m.EncryptedMessage),
            Created = m.Created.ToTimestamp(),
            Edited = m.Edited?.ToTimestamp(),
            KeyVersion = m.KeyVersion,
            Deleted = m.Deleted
        }));

        return chats;
    }

    /// <summary>
    ///     Receive new and updates messages of the chats.
    /// </summary>
    /// <param name="request">Request parameter.</param>
    /// <param name="responseStream">Stream to write reponse.</param>
    /// <param name="context">Connection context.</param>
    public override async Task ReceiveMessages(ChatReceiveRequest request, IServerStreamWriter<ChatNotification> responseStream, ServerCallContext context)
    {
        // Reveive id of the user
        string? idString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idString, out var userId))
            return;

        // Create new cliend id
        var id = Guid.NewGuid();
        var reader = await _notificationHandler.RegisterAsync(id, userId, context.CancellationToken).ConfigureAwait(false);

        try
        {
            // Read all updates
            await foreach (var notification in reader.ReadAllAsync(context.CancellationToken).ConfigureAwait(false))
            {
                // Send updates to client
                await responseStream.WriteAsync(notification, context.CancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            // Remove client from handler
            await _notificationHandler.UnregisterAsync(id).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Get encrypted shared keys between users.
    /// </summary>
    /// <param name="request">Request parameter.</param>
    /// <param name="context">Connection context.</param>
    /// <returns>Collection of encrypted shared keys.</returns>
    public override async Task<CryptographicKeysReponse> GetCryptographicKeys(CryptographicKeysRequest request, ServerCallContext context)
    {
        // Reveive id of the user
        string? userIdString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return new CryptographicKeysReponse();

        // Check if a valid target id is provied
        if (!Guid.TryParse(request.TargetId, out var targetId))
            return new CryptographicKeysReponse();

        uint max = request.HasMaximumVersion ? request.MaximumVersion : int.MaxValue;

        // Get keys
        var keys = await _chatRepository
            .GetCryptographicKeysAsync(userId, targetId, request.MinimumVersion, max)
            .ConfigureAwait(false);

        // Convert to communication data structure
        var response = new CryptographicKeysReponse();
        response.Keys.AddRange(keys.Select(k => new CryptographicKeysReponse.Types.CryptographicKey
        {
            Key = UnsafeByteOperations.UnsafeWrap(k.EncryptedKey),
            Version = k.Version,
            PublicKeyVersion = k.PublicKeyVersion
        }));

        return response;
    }

    /// <summary>
    ///     Update the current encrypted shared keys between users.
    /// </summary>
    /// <param name="request">Request parameter.</param>
    /// <param name="context">Connection context.</param>
    /// <returns>Whether the update was successful and an optional new shared key version.</returns>
    public override async Task<CryptographicKeysUpdateResponse> UpdateCryptographicKeys(CryptographicKeysUpdateRequest request, ServerCallContext context)
    {
        // Reveive id of the user
        string? userIdString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return new CryptographicKeysUpdateResponse { Success = false };

        // Check if a valid target id is provied
        if (!Guid.TryParse(request.TargetId, out var targetId))
            return new CryptographicKeysUpdateResponse { Success = false };

        // Update keys
        uint version = await _chatRepository
            .UpdateCryptographicKeysAsync(userId, targetId, request.OwnEncryptedKey.Memory, request.OwnPublicKeyVersion, request.TargetEncryptedKey.Memory, request.TargetPublicKeyVersion)
            .ConfigureAwait(false);

        if (version > 0)
            _logger.LogInformation("Cryptographic Keys updated between '{UserId}' and '{TargetId}'", userId, targetId);

        return new CryptographicKeysUpdateResponse { Success = version > 0, Version = version };
    }
}
