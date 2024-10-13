using Dapper;
using EncryptedChat.Common;
using EncryptedChat.Server.Chats;
using EncryptedChat.Server.Database;
using EncryptedChat.Server.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var services = builder.Services;

services.AddSingleton<IDbConnectionFactory>(_ => new SqliteDbConnectionFactory(config.GetConnectionString("Sqlite")!));
DefaultTypeMap.MatchNamesWithUnderscores = true;
SqlMapper.AddTypeHandler(new GuidHandler());

services.AddSingleton<IUserRepository, UserRepository>();
services.AddSingleton<IUserUpdateNotificationHandler, UserUpdateNotificationHandler>();

services.AddSingleton<IChatRepository, ChatRepository>();
services.AddSingleton<IChatNotificationHandler, ChatNotificationHandler>();

services.AddGrpc();

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
#if DEBUG
        options.RequireHttpsMetadata = false;
#endif
        
        options.Authority = config["Authentication:Issuer"];
        options.Audience = config["Authentication:Audience"];

        options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(5);
    });

services.AddAuthorization();

var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<ChatService>();
app.MapGrpcService<UserService>();

app.Run();
