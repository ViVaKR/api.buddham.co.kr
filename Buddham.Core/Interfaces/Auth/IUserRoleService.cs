using Buddham.Core.Common.Results;
using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Auth;

public interface IUserRoleService
    : IBaseService<string, UserRoleSchema.Request, UserRoleSchema.Response>,
        ICursorQueryable<UserRoleSchema.Response>,
        IOffsetQueryable<UserRoleSchema.Response>
{
    Task<Result<UserRoleSchema.Response>> GetUserInfoByAdminAsync(string id);
}
