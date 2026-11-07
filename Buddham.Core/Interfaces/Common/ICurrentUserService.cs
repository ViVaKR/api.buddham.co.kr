namespace Buddham.Core.Interfaces.Common;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsAuthenticatied { get; }
    bool IsAdmin { get; }
}
