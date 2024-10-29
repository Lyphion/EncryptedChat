using System.Data;

namespace EncryptedChat.Server.Database;

/// <summary>
///     Factory to create a new database connection.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    ///     Create and open a new database connection.
    /// </summary>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>Created and opened database connection.</returns>
    Task<IDbConnection> CreateConnectionAsync(CancellationToken token = default);
}
