using CSharpFunctionalExtensions;
using Dapper;
using EncryptedChat.Server.Database;

namespace EncryptedChat.Server.Users;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<User>> GetUsersAsync(CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        return await connection.QueryAsync<User>(
            "SELECT * FROM users;"
        ).ConfigureAwait(false);
    }

    public async Task<Maybe<User>> GetUserAsync(Guid id, CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        return await connection.QueryFirstOrDefaultAsync<User>(
            """
            SELECT * FROM users
            WHERE id = @id LIMIT 1;
            """,
            new { id }
        ).ConfigureAwait(false);
    }

    public async Task<bool> CreateUserAsync(User user, CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        int result = await connection.ExecuteAsync(
            """
            INSERT INTO users (id, name, public_key)
            VALUES (@Id, @Name, @PublicKey);
            """,
            user
        ).ConfigureAwait(false);

        return result > 0;
    }

    public async Task<bool> UpdateNameAsync(Guid id, string name, CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        int result = await connection.ExecuteAsync(
            """
            UPDATE users
            SET name = @name
            WHERE id = @id;
            """,
            new { id, name }
        );

        return result > 1;
    }

    public async Task<bool> UpdatePublicKeyAsync(Guid id, ReadOnlyMemory<byte> key, CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        int result = await connection.ExecuteAsync(
            """
            UPDATE users
            SET public_key = @key
            WHERE id = @id;
            """,
            new { id, key }
        ).ConfigureAwait(false);

        return result > 1;
    }
}
