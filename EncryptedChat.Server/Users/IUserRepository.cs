namespace EncryptedChat.Server.Users;

/// <summary>
///     Repository the save and receive user information.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    ///     Receive all users with the provided name part.
    /// </summary>
    /// <param name="namePart">Part of the username.</param>
    /// <param name="limit">Maximum of users the return.</param>
    /// <param name="offset">Offset in the list.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>Collection of users matching the request.</returns>
    Task<IEnumerable<User>> GetUsersAsync(string? namePart, uint limit = int.MaxValue, uint offset = 0, CancellationToken token = default);

    /// <summary>
    ///     Receive the user with the specified id.
    /// </summary>
    /// <param name="id">Id of the user.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>User with the id.</returns>
    Task<User?> GetUserAsync(Guid id, CancellationToken token = default);

    /// <summary>
    ///     Create a new user with the specified properties.
    /// </summary>
    /// <param name="id">Id of the user.</param>
    /// <param name="name">Name of the user.</param>
    /// <param name="key">Public key of the user.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>Version of the key, or <c>0</c> if the operation failed.</returns>
    Task<uint> CreateUserAsync(Guid id, string name, ReadOnlyMemory<byte> key, CancellationToken token = default);

    /// <summary>
    ///     Update user with specified properties.
    /// </summary>
    /// <param name="id">Id of the user.</param>
    /// <param name="name">Name of the user.</param>
    /// <param name="key">Public key of the user.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>Version of the key, or <c>0</c> if the operation failed.</returns>
    Task<uint> UpdateUserAsync(Guid id, string name, ReadOnlyMemory<byte> key, CancellationToken token = default);
}
