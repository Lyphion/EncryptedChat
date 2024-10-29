using System.Data;
using Dapper;

namespace EncryptedChat.Server.Database;

/// <summary>
///     Mapper to convert bewteen <see cref="Guid"/> and bytes.
/// </summary>
public sealed class GuidHandler : SqlMapper.TypeHandler<Guid>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value.ToByteArray();

    /// <inheritdoc />
    public override Guid Parse(object value) => new((byte[]) value);
}
