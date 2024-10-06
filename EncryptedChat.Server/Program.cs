using EncryptedChat.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<ChatService>();
app.MapGrpcService<UserService>();

app.Run();
