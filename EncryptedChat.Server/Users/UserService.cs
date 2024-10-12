using System.Security.Claims;
using EncryptedChat.Common;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace EncryptedChat.Server.Users;

[Authorize]
public sealed class UserService : Common.User.UserBase
{
    private readonly ILogger<UserService> _logger;

    private readonly IUserRepository _userRepository;

    public UserService(ILogger<UserService> logger, IUserRepository userRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
    }

    public override async Task<UsersReponse> GetUsers(UsersRequest request, ServerCallContext context)
    {
        var users = await _userRepository
            .GetUsersAsync()
            .ConfigureAwait(false);

        var usersReponse = new UsersReponse();
        usersReponse.Users.AddRange(users.Select(u => new UserResponse
        {
            Id = u.Id.ToString(),
            Name = u.Name,
            PublicKey = ByteString.CopyFrom(u.PublicKey)
        }));

        return usersReponse;
    }

    public override async Task<UserResponse> GetUser(UserRequest request, ServerCallContext context)
    {
        if (Guid.TryParse(request.Id, out var id))
            return new UserResponse();

        var optionen = await _userRepository
            .GetUserAsync(id)
            .ConfigureAwait(false);

        if (!optionen.TryGetValue(out var user))
            return new UserResponse();

        return new UserResponse
        {
            Id = user.Id.ToString(),
            Name = user.Name,
            PublicKey = ByteString.CopyFrom(user.PublicKey)
        };
    }

    public override async Task<PublicKeyResponse> UpdatePublicKey(PublicKeyRequest request, ServerCallContext context)
    {
        string? idString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idString, out var id))
            return new PublicKeyResponse { Success = false };

        bool success = await _userRepository
            .UpdatePublicKeyAsync(id, request.PublicKey.Memory)
            .ConfigureAwait(false);

        if (success)
            _logger.LogInformation("Public key updated for '{Id}'", id);

        return new PublicKeyResponse { Success = success };
    }
}
