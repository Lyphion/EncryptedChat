using CSharpFunctionalExtensions;

namespace EncryptedChat.Server.Users;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetUsersAsync(CancellationToken token = default);

    Task<Maybe<User>> GetUserAsync(Guid id, CancellationToken token = default);

    Task<bool> CreateUserAsync(User user, CancellationToken token = default);

    Task<bool> UpdateNameAsync(Guid id, string name, CancellationToken token = default);

    Task<bool> UpdatePublicKeyAsync(Guid id, ReadOnlyMemory<byte> key, CancellationToken token = default);
}
