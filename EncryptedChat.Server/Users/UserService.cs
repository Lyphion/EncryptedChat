using System.Security.Claims;
using EncryptedChat.Common;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace EncryptedChat.Server.Users;

/// <summary>
///     Service to handle gRPC requests for user operations.
/// </summary>
[Authorize]
public sealed class UserService : Common.User.UserBase
{
    /// <summary>
    ///     Logger for this service.
    /// </summary>
    private readonly ILogger<UserService> _logger;

    /// <summary>
    ///     Repository for managing users.
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    ///     Handler to manage communication between clients.
    /// </summary>
    private readonly IUserUpdateNotificationHandler _notificationHandler;

    /// <summary>
    ///     Create a new <see cref="UserService"/> to handle gRPC requests.
    /// </summary>
    /// <param name="logger">Logger for this service.</param>
    /// <param name="userRepository">Repository for managing users.</param>
    /// <param name="notificationHandler">Handler to manage communication between clients.</param>
    public UserService(ILogger<UserService> logger, IUserRepository userRepository, IUserUpdateNotificationHandler notificationHandler)
    {
        _logger = logger;
        _userRepository = userRepository;
        _notificationHandler = notificationHandler;
    }

    /// <summary>
    ///     Receive all users with the provided name part.
    /// </summary>
    /// <param name="request">Request parameter.</param>
    /// <param name="context">Connection context.</param>
    /// <returns>Collection of user matching the request.</returns>
    public override async Task<UsersReponse> GetUsers(UsersRequest request, ServerCallContext context)
    {
        // Receive users from repository
        var users = await _userRepository
            .GetUsersAsync(request.HasNamePart ? request.NamePart : null, request.HasLimit ? request.Limit : int.MaxValue, request.Offset)
            .ConfigureAwait(false);

        // Convert to communication data structure
        var usersReponse = new UsersReponse();
        usersReponse.Users.AddRange(users.Select(u => new UserResponse
        {
            Id = u.Id.ToString(),
            Name = u.Name,
            PublicKey = UnsafeByteOperations.UnsafeWrap(u.PublicKey),
            PublicKeyVersion = u.PublicKeyVersion
        }));

        return usersReponse;
    }

    /// <summary>
    ///     Receive the user with the specified id. 
    /// </summary>
    /// <param name="request">Request parameter.</param>
    /// <param name="context">Connection context.</param>
    /// <returns>User with the matching id.</returns>
    public override async Task<UserResponse> GetUser(UserRequest request, ServerCallContext context)
    {
        // Check if a valid id is provied
        if (Guid.TryParse(request.Id, out var id))
            return new UserResponse();

        // Reveive user from repository
        var user = await _userRepository
            .GetUserAsync(id)
            .ConfigureAwait(false);

        // Check if user existed
        if (user is null)
            return new UserResponse();

        // Convert to communication data structure
        return new UserResponse
        {
            Id = user.Id.ToString(),
            Name = user.Name,
            PublicKey = UnsafeByteOperations.UnsafeWrap(user.PublicKey),
            PublicKeyVersion = user.PublicKeyVersion
        };
    }

    /// <summary>
    ///     Update own user properties.
    /// </summary>
    /// <param name="request">Request parameter.</param>
    /// <param name="context">Connection context.</param>
    /// <returns>Whether the update was successful and an optional new public key version.</returns>
    public override async Task<UserUpdateResponse> UpdateUser(UserUpdateRequest request, ServerCallContext context)
    {
        // Reveive id of the user
        string? idString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idString, out var id))
            return new UserUpdateResponse { Success = false };

        uint keyVersion;
        var notification = new UserUpdateNotification { Id = id.ToString() };

        // Check if this is a new user -> create new one
        var user = await _userRepository.GetUserAsync(id).ConfigureAwait(false);
        if (user is null)
        {
            // Check if all properties are provied
            if (request is { HasName: false, HasPublicKey: false })
                return new UserUpdateResponse { Success = false };

            // Create user
            keyVersion = await _userRepository
                .CreateUserAsync(id, request.Name, request.PublicKey.Memory)
                .ConfigureAwait(false);

            // Check if creation was succesful
            if (keyVersion == 0)
                return new UserUpdateResponse { Success = false };

            _logger.LogInformation("User '{Id}' create", id);

            // Set types of update
            notification.Type.Add(UserUpdateNotification.Types.UpdateType.Name);
            notification.Type.Add(UserUpdateNotification.Types.UpdateType.PublicKey);
        }
        else
        {
            // Update user
            keyVersion = await _userRepository
                .UpdateUserAsync(id, request.HasName ? request.Name : user.Name, request.HasPublicKey ? request.PublicKey.Memory : user.PublicKey)
                .ConfigureAwait(false);

            // Check if update was succesful
            if (keyVersion == 0)
                return new UserUpdateResponse { Success = false };

            _logger.LogInformation("User '{Id}' updated", id);

            // Set types of update
            if (request.HasName)
                notification.Type.Add(UserUpdateNotification.Types.UpdateType.Name);
            if (request.HasPublicKey)
                notification.Type.Add(UserUpdateNotification.Types.UpdateType.PublicKey);
        }

        // Notify connected clients in the background
        _ = _notificationHandler.PublishNotificationAsync(notification).ConfigureAwait(false);

        return new UserUpdateResponse { Success = true, PublicKeyVersion = keyVersion };
    }

    /// <summary>
    ///     Receive updates on users.
    /// </summary>
    /// <param name="request">Request parameter.</param>
    /// <param name="responseStream">Stream to write reponse.</param>
    /// <param name="context">Connection context.</param>
    public override async Task ReceiveUserUpdates(UserReceiveRequest request, IServerStreamWriter<UserUpdateNotification> responseStream, ServerCallContext context)
    {
        // Reveive id of the user
        string? idString = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idString, out var userId))
            return;

        // Create new cliend id
        var id = Guid.NewGuid();
        var reader = _notificationHandler.Register(id, userId);

        try
        {
            // Read all notications
            await foreach (var notification in reader.ReadAllAsync(context.CancellationToken).ConfigureAwait(false))
            {
                // Send notications to client
                await responseStream.WriteAsync(notification, context.CancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            // Remove client from handler
            _notificationHandler.Unregister(id);
        }
    }
}
