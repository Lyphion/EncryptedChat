using System.Data.Common;
using Dapper;
using EncryptedChat.Server.Database;

namespace EncryptedChat.Server.Users;

public sealed class UserRepository : IUserRepository
{
    private readonly ILogger<UserRepository> _logger;

    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(ILogger<UserRepository> logger, IDbConnectionFactory connectionFactory)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<User>> GetUsersAsync(string? namePart, uint limit = int.MaxValue, uint offset = 0, CancellationToken token = default)
    {
        try
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
        catch (DbException ex)
        {
            _logger.LogError(ex, "Failed to get users");
            return Array.Empty<User>();
        }
    }

    public async Task<User?> GetUserAsync(Guid id, CancellationToken token = default)
    {
        try
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
        catch (DbException ex)
        {
            _logger.LogError(ex, "Failed to get user '{Id}'", id);
            return null;
        }
    }

    public async Task<bool> CreateUserAsync(Guid id, string name, ReadOnlyMemory<byte> key, CancellationToken token = default)
    {
        try
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
        catch (DbException ex)
        {
            _logger.LogError(ex, "Failed to create user '{Id}'", id);
            return false;
        }
    }

    public async Task<bool> UpdateUserAsync(Guid id, string name, ReadOnlyMemory<byte> key, CancellationToken token = default)
    {
        try
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
        catch (DbException ex)
        {
            _logger.LogError(ex, "Failed to update user '{Id}'", id);
            return false;
        }
    }
}
