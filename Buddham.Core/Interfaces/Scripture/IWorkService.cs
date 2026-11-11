using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Scripture;

/// <summary>경(작품 단위)과 그 식별자.</summary>
public interface IWorkService
{
    /// <summary>관리자·일반 공용 목록. 집성 또는 그룹으로 좁힌다. (SearchKeyword: 제목·코드)</summary>
    Task<OffsetPagedResult<ScriptureSchema.Work.Response>> GetWorksAsync(
        long? collectionId, long? groupId, OffsetPagedQuery query, CancellationToken ct);

    /// <param name="includeUnpublishedEditions">관리자만 true. 일반 독자에게는 공개 판본만 보인다.</param>
    Task<Result<ScriptureSchema.Work.Detail>> GetDetailAsync(long id, bool includeUnpublishedEditions, CancellationToken ct);

    /// <summary>생성. Request.Identifiers 로 식별자를 함께 만든다.</summary>
    Task<Result<ScriptureSchema.Work.Detail>> CreateAsync(ScriptureSchema.Work.Request request, CancellationToken ct);

    /// <summary>수정. 식별자는 전용 메서드로만 바꾼다(Request.Identifiers 는 비워야 함).</summary>
    Task<Result<ScriptureSchema.Work.Detail>> UpdateAsync(long id, ScriptureSchema.Work.Request request, CancellationToken ct);

    Task<Result> DeleteAsync(long id, CancellationToken ct);

    Task<Result<ScriptureSchema.Identifier.Response>> AddIdentifierAsync(long workId, ScriptureSchema.Identifier.Request request, CancellationToken ct);
    Task<Result> SetPrimaryIdentifierAsync(long workId, long identifierId, CancellationToken ct);
    Task<Result> RemoveIdentifierAsync(long workId, long identifierId, CancellationToken ct);

    /// <summary>"SN 12.2", "t99" 같은 번호로 경 찾기. 체계가 달라 겹칠 수 있어 목록으로 돌려준다.</summary>
    Task<Result<IReadOnlyList<ScriptureSchema.Identifier.Match>>> FindByIdentifierAsync(string value, string? scheme, CancellationToken ct);
}
