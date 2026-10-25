using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Shared.Infrastructure;

public abstract class BaseController : ControllerBase
{
    protected Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected string UserIdString => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    protected string UserEmail => User.FindFirstValue(ClaimTypes.Email)!;
}
