using EncryptedChat.Common;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using IdentityModel.Client;

var credentials = CallCredentials.FromInterceptor(async (context, metadata) =>
{
    using var client = new HttpClient();
    var token = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
    {
        Address = "http://127.0.0.1:8080/realms/chat/protocol/openid-connect/token",
        ClientId = "chat-client",
        UserName = "test",
        Password = "test",
        Scope = "chat"
    }).ConfigureAwait(false);

    metadata.Add("Authorization", $"Bearer {token.AccessToken}");
});

using var channel = GrpcChannel.ForAddress("https://localhost:7001", new GrpcChannelOptions
{
    Credentials = ChannelCredentials.Create(new SslCredentials(), credentials),
});

var chatClient = new Chat.ChatClient(channel);
var userClient = new User.UserClient(channel);

var users = await userClient.GetUsersAsync(new UsersRequest()).ConfigureAwait(false);

var response = await chatClient.SendMessageAsync(new ChatMessageRequest
{
    TargetId = Guid.NewGuid().ToString(),
    EncryptedMessage = ByteString.CopyFromUtf8("Hello World"),
    KeyVersion = 1
}).ConfigureAwait(false);
