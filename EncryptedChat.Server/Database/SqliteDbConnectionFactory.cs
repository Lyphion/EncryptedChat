using System.Data;
using System.Data.SQLite;

namespace EncryptedChat.Server.Database;

/// <summary>
///     Factory to create a Sqlite database connection.
/// </summary>
public sealed class SqliteDbConnectionFactory : IDbConnectionFactory
{
    /// <summary>
    ///     Connection string for accessing the database.
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    ///     Create a new <see cref="SqliteDbConnectionFactory"/> to create database connections.
    /// </summary>
    /// <param name="connectionString">Connection string for accessing the database.</param>
    public SqliteDbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken token = default)
    {
        var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync(token).ConfigureAwait(false);
        return connection;
    }
}
