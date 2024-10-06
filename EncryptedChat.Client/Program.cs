using EncryptedChat.Common;
using Grpc.Net.Client;

using var channel = GrpcChannel.ForAddress("https://localhost:7001");
var chatClient = new Chat.ChatClient(channel);
var userClient = new User.UserClient(channel);



