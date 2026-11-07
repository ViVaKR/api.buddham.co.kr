using Buddham.Core.Interfaces.Cache;
using Buddham.Core.Interfaces.Common;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Services.Crud;

/// <summary>
/// 모든 CRUD 서비스가 공통으로 받는 의존성 묶음.
/// 파생 서비스 생성자가 (context, deps) 두 개로 짧아진다. Scoped 로 등록한다.
/// </summary>
public sealed class CrudDependencies(
    ICurrentUserService currentUser,
    IEntityAuthorizationService authorization,
    IEntityCacheFactory cacheFactory,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory)
{
    public ICurrentUserService CurrentUser { get; } = currentUser;
    public IEntityAuthorizationService Authorization { get; } = authorization;
    public IEntityCacheFactory CacheFactory { get; } = cacheFactory;
    public TimeProvider TimeProvider { get; } = timeProvider;
    public ILoggerFactory LoggerFactory { get; } = loggerFactory;
}
