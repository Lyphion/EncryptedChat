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
            .GetUsersAsync(request.HasNamePart ? request.NamePart : null, request.HasLimit ? request.Limit : int.MaxValue, request.Offset)
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

        var user = await _userRepository
            .GetUserAsync(id)
            .ConfigureAwait(false);

        if (user is null)
            return new UserResponse();

        return new UserResponse
        {
            Id = user.Id.ToString(),
            Name = user.Name,
            PublicKey = ByteString.CopyFrom(user.PublicKey)
        };
    }

    public override async Task<UserUpdateResponse> UpdateUser(UserUpdateRequest request, ServerCallContext context)
    {
        string? idString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idString, out var id))
            return new UserUpdateResponse { Success = false };

        bool success;
        var user = await _userRepository.GetUserAsync(id).ConfigureAwait(false);
        if (user is null)
        {
            if (request is { HasName: false, HasPublicKey: false })
                return new UserUpdateResponse { Success = false };

            success = await _userRepository
                .CreateUserAsync(id, request.Name, request.PublicKey.Memory)
                .ConfigureAwait(false);

            if (success)
                _logger.LogInformation("User '{Id}' create", id);
            
            // TODO Send user update

            return new UserUpdateResponse { Success = success };
        }

        success = await _userRepository
            .UpdateUserAsync(id, request.HasName ? request.Name : user.Name, request.HasPublicKey ? request.PublicKey.Memory : user.PublicKey)
            .ConfigureAwait(false);

        if (success)
            _logger.LogInformation("User '{Id}' updated", id);
        
        // TODO Send user update

        return new UserUpdateResponse { Success = success };
    }

    public override async Task ReceiveUserUpdates(UserReceiveRequest request, IServerStreamWriter<UserUpdateNotification> responseStream, ServerCallContext context)
    {
        // TODO Send messages
    }
}
