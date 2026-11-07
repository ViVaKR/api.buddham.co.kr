using System.Security.Claims;
using Buddham.Core.Interfaces.Common;
using Buddham.Shared.Constants;
using Microsoft.AspNetCore.Http;

namespace Buddham.Shared.Services.Common;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly ClaimsPrincipal? User = httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public bool IsAuthenticatied => User?.Identity?.IsAuthenticated == true;

    public bool IsAdmin =>
        User?.IsInRole(UserRoles.Admin) == true ||
        User?.HasClaim("role", UserRoles.Admin) == true;
}
