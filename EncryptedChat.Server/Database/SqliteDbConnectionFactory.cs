using System.Data;
using System.Data.SQLite;

namespace EncryptedChat.Server.Database;

public sealed class SqliteDbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteDbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken token = default)
    {
        var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync(token).ConfigureAwait(false);
        return connection;
    }
}
