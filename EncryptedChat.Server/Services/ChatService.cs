using EncryptedChat.Common;
using Grpc.Core;

namespace EncryptedChat.Server.Services;

public sealed class ChatService : Chat.ChatBase
{
    public override Task<ChatMessageResponse> SendMessage(ChatMessageRequest request, ServerCallContext context)
    {
        return base.SendMessage(request, context);
    }

    public override Task<DeleteChatMessageResponse> DeleteMessage(DeleteChatMessageRequest request, ServerCallContext context)
    {
        return base.DeleteMessage(request, context);
    }

    public override Task<ChatResponse> GetMessages(ChatRequest request, ServerCallContext context)
    {
        return base.GetMessages(request, context);
    }

    public override Task ReceiveMessages(ChatReceiveRequest request, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
    {
        return base.ReceiveMessages(request, responseStream, context);
    }

    public override Task<CryptographicKeysReponse> GetCryptographicKeys(CryptographicKeysRequest request, ServerCallContext context)
    {
        return base.GetCryptographicKeys(request, context);
    }

    public override Task<CryptographicKeysUpdateResponse> UpdateCryptographicKeys(CryptographicKeysUpdateRequest request, ServerCallContext context)
    {
        return base.UpdateCryptographicKeys(request, context);
    }
}
