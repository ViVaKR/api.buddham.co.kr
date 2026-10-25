using Buddham.Core.Common.Results;
using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Auth;

public interface IUserService
    : IBaseService<string, UserSchema.Request, UserSchema.Response>,
        ICursorQueryable<UserSchema.Response>,
        IOffsetQueryable<UserSchema.Response>
{
    Task<Result> RestoreAsync(string id);
    Task<Result<UserSchema.Response>> GetUserInfoAsync(string userId);
    Task<Result<UserSchema.Response>> GetUserInfoByAdminAsync(string id);
    Task<Result<IEnumerable<UserSchema.Response>>> GetUsersForAdminAsync(CancellationToken ct);
}
