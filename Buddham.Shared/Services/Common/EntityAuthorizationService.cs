using Buddham.Core.Interfaces.Common;

namespace Buddham.Shared.Services.Common;

public sealed class EntityAuthorizationService(ICurrentUserService currentUser) : IEntityAuthorizationService
{
    public bool CanModify(Guid? userId) =>
        currentUser.IsAdmin || (currentUser.UserId.HasValue && currentUser.UserId == userId);
}
