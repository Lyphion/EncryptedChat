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

    public async Task<IEnumerable<User>> GetUsersAsync(string? namePart, uint limit = int.MaxValue, uint offset = 0, CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        if (namePart is null)
        {
            return await connection.QueryAsync<User>(
                """
                SELECT * FROM users
                LIMIT @limit OFFSET @offset;
                """,
                new { limit, offset }
            ).ConfigureAwait(false);
        }
        
        return await connection.QueryAsync<User>(
            """
            SELECT * FROM users
            WHERE name LIKE @namePart ESCAPE '!'
            LIMIT @limit OFFSET @offset;
            """,
            new { namePart = $"%{namePart.Replace("%", "!%")}%", limit, offset }
        ).ConfigureAwait(false);
    }

    public async Task<User?> GetUserAsync(Guid id, CancellationToken token = default)
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

    public async Task<bool> CreateUserAsync(Guid id, string name, ReadOnlyMemory<byte> key, CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        int result = await connection.ExecuteAsync(
            """
            INSERT INTO users (id, name, public_key)
            VALUES (@id, @name, @key);
            """,
            new { id, name, key }
        ).ConfigureAwait(false);

        return result > 0;
    }

    public async Task<bool> UpdateUserAsync(Guid id, string name, ReadOnlyMemory<byte> key, CancellationToken token = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(token).ConfigureAwait(false);

        int result = await connection.ExecuteAsync(
            """
            UPDATE users
            SET name = @name, public_key = @key
            WHERE id = @id;
            """,
            new { id, name, key }
        ).ConfigureAwait(false);

        return result > 1;
    }
}
