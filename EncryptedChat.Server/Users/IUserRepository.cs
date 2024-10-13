﻿namespace EncryptedChat.Server.Users;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetUsersAsync(string? namePart, uint limit = int.MaxValue, uint offset = 0, CancellationToken token = default);

    Task<User?> GetUserAsync(Guid id, CancellationToken token = default);

    Task<bool> CreateUserAsync(Guid id, string name, ReadOnlyMemory<byte> key, CancellationToken token = default);

    Task<bool> UpdateUserAsync(Guid id, string name, ReadOnlyMemory<byte> key, CancellationToken token = default);
}
