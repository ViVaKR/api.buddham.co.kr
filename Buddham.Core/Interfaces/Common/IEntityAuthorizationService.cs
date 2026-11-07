namespace Buddham.Core.Interfaces.Common;

public interface IEntityAuthorizationService
{
    bool CanModify(Guid? userId);
}
