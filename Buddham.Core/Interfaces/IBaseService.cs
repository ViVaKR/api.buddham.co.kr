using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;

namespace Buddham.Core.Interfaces;

/// <summary>
/// 베이스 서비스
/// long, Guid, string 모두 수용
/// </summary>
public interface IBaseService<TId, TRequest, TResponse> where TId : IEquatable<TId>
{
    /// <summary>
    /// 전체 목록조회
    /// </summary>
    Task<Result<IEnumerable<TResponse>>> GetAsync();

    /// <summary>
    /// 커서 목록조회
    /// (키워드 없음: CreateAt + Id 커서 / 키워드 있음: TsVector 전문검색)
    /// </summary>
    // Task<CursorPagedResult<TResponse>> GetCursorListAsync(CursorPagedQuery query, CancellationToken ct);

    /// <summary>
    /// 오프셋 페이지 목록 조회
    /// </summary>
    // Task<OffsetPagedResult<TResponse>> GetOffsetListAsync(OffsetPagedQuery query, CancellationToken ct);

    /// <summary>
    /// 의미론적 유사도 기반 커서 목록 조회 (pgvector / bge-m3)
    /// 글자가 달라도 뜻이 비슷한 문서를 코사인 유사도로 탐색
    /// </summary>
    // Task<CursorPagedResult<TResponse>> GetSemanticListAsync(SemanticSearchQuery query, CancellationToken ct);


    // ══════════════════════════════════════════
    // ✏️ CRUD
    // ══════════════════════════════════════════

    /// <summary>
    /// 생성
    /// </summary>
    Task<Result> CreateAsync(TRequest request, CancellationToken ct);

    /// <summary>
    /// 수정
    /// </summary>
    Task<Result> UpdateAsync(TId id, TRequest request, CancellationToken ct);

    /// <summary>
    /// 삭제
    /// </summary>
    Task<Result> DeleteAsync(TId id);
}

// ── 무한 스크롤/피드형 목록이 필요한 곳만 ──
public interface ICursorQueryable<TResponse>
{
    Task<CursorPagedResult<TResponse>> GetCursorListAsync(CursorPagedQuery query, CancellationToken ct);
}

// ── 관리자 페이지네이션(페이지 번호)이 필요한 곳만 ──
public interface IOffsetQueryable<TResponse>
{
    Task<OffsetPagedResult<TResponse>> GetOffsetListAsync(OffsetPagedQuery query, CancellationToken ct);
}

// ── Embedding 컬럼을 실제로 가진 엔티티만 ──
public interface ISemanticQueryable<TResponse>
{
    Task<CursorPagedResult<TResponse>> GetSemanticListAsync(SemanticSearchQuery query, CancellationToken ct);
}

// ── 유저 조회가 필요한 곳만 (지난번 것) ──
public interface IUserQueryableService<TId, TResponse> where TId : IEquatable<TId>
{
    Task<Result<TResponse>> GetUserInfoByAdminAsync(TId id);
    Task<Result<TResponse>> GetUserInfoAsync(TId userId);
}

public interface IDetailQueryableService<TId, TResponse> where TId : IEquatable<TId>
{
    /// <summary>
    /// 단건 조회
    /// </summary>
    Task<Result<TResponse>> GetByIdAsync(TId id);
}
