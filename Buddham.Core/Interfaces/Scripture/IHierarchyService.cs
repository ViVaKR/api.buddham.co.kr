using Buddham.Core.Common.Results;
using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Scripture;

/// <summary>
/// 전승 → (판본계통 → 부 → 집성 → 그룹) 구조 관리.
/// IBaseService 대신 전용 인터페이스를 쓴다: 한 서비스가 다섯 엔터티를 다루고, 생성 응답에 새 Id 가 필요하기 때문.
/// 쓰기 메서드는 모두 갱신된 Response 를 돌려준다.
/// </summary>
public interface IHierarchyService
{
    // 전승 계통
    Task<Result<IReadOnlyList<ScriptureSchema.Tradition.Response>>> GetTraditionsAsync(CancellationToken ct);
    Task<Result<ScriptureSchema.Tradition.Response>> CreateTraditionAsync(ScriptureSchema.Tradition.Request request, CancellationToken ct);
    Task<Result<ScriptureSchema.Tradition.Response>> UpdateTraditionAsync(long id, ScriptureSchema.Tradition.Request request, CancellationToken ct);
    Task<Result> DeleteTraditionAsync(long id, CancellationToken ct);

    // 판본계통
    Task<Result<IReadOnlyList<ScriptureSchema.Canon.Response>>> GetCanonsAsync(CancellationToken ct);
    Task<Result<ScriptureSchema.Canon.Response>> CreateCanonAsync(ScriptureSchema.Canon.Request request, CancellationToken ct);
    Task<Result<ScriptureSchema.Canon.Response>> UpdateCanonAsync(long id, ScriptureSchema.Canon.Request request, CancellationToken ct);
    Task<Result> DeleteCanonAsync(long id, CancellationToken ct);

    // 부(部)
    Task<Result<IReadOnlyList<ScriptureSchema.Division.Response>>> GetDivisionsAsync(long? canonId, CancellationToken ct);
    Task<Result<ScriptureSchema.Division.Response>> CreateDivisionAsync(ScriptureSchema.Division.Request request, CancellationToken ct);
    Task<Result<ScriptureSchema.Division.Response>> UpdateDivisionAsync(long id, ScriptureSchema.Division.Request request, CancellationToken ct);
    Task<Result> DeleteDivisionAsync(long id, CancellationToken ct);

    // 집성
    Task<Result<IReadOnlyList<ScriptureSchema.Collection.Response>>> GetCollectionsAsync(long? divisionId, CancellationToken ct);
    Task<Result<ScriptureSchema.Collection.Response>> CreateCollectionAsync(ScriptureSchema.Collection.Request request, CancellationToken ct);
    Task<Result<ScriptureSchema.Collection.Response>> UpdateCollectionAsync(long id, ScriptureSchema.Collection.Request request, CancellationToken ct);
    Task<Result> DeleteCollectionAsync(long id, CancellationToken ct);

    // 그룹
    Task<Result<IReadOnlyList<ScriptureSchema.Group.Response>>> GetGroupsAsync(long? collectionId, CancellationToken ct);
    Task<Result<ScriptureSchema.Group.Response>> CreateGroupAsync(ScriptureSchema.Group.Request request, CancellationToken ct);
    Task<Result<ScriptureSchema.Group.Response>> UpdateGroupAsync(long id, ScriptureSchema.Group.Request request, CancellationToken ct);
    Task<Result> DeleteGroupAsync(long id, CancellationToken ct);
}
