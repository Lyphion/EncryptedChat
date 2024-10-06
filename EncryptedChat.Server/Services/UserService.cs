using EncryptedChat.Common;
using Grpc.Core;

namespace EncryptedChat.Server.Services;

public sealed class UserService : User.UserBase
{
    public override Task<UsersReponse> GetUsers(UsersRequest request, ServerCallContext context)
    {
        return base.GetUsers(request, context);
    }

    public override Task<UserResponse> GetUser(UserRequest request, ServerCallContext context)
    {
        return base.GetUser(request, context);
    }

    public override Task<PublicKeyResponse> UpdatePublicKey(PublicKeyRequest request, ServerCallContext context)
    {
        return base.UpdatePublicKey(request, context);
    }
}
