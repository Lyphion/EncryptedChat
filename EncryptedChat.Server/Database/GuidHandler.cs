using System.Data;
using Dapper;

namespace EncryptedChat.Server.Database;

public sealed class GuidHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value.ToByteArray();

    public override Guid Parse(object value) => new((byte[]) value);
}
