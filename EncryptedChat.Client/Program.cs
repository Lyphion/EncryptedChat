using EncryptedChat.Client;
using EncryptedChat.Client.Authentication;
using EncryptedChat.Client.Services;
using EncryptedChat.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;
var services = builder.Services;

services.AddSingleton<ITokenProvider, TokenProvider>();
services.Configure<TokenOptions>(config.GetSection("Authentication"));
services.AddHttpClient(TokenProvider.ClientName);

services.AddHostedService<CommunicationService>();

services.AddGrpcClient<Chat.ChatClient>(options => { options.Address = new Uri(config["Server:Uri"]!); })
    .AddCallCredentials(async (context, metadata, serviceProvider) =>
    {
        var provider = serviceProvider.GetRequiredService<ITokenProvider>();
        string token = await provider.GetTokenAsync(context.CancellationToken);
        metadata.Add("Authorization", $"Bearer {token}");
    });

services.AddGrpcClient<User.UserClient>(options => { options.Address = new Uri(config["Server:Uri"]!); })
    .AddCallCredentials(async (context, metadata, serviceProvider) =>
    {
        var provider = serviceProvider.GetRequiredService<ITokenProvider>();
        string token = await provider.GetTokenAsync(context.CancellationToken);
        metadata.Add("Authorization", $"Bearer {token}");
    });

var app = builder.Build();

app.Run();
