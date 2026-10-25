namespace Buddham.Core.Common.Results;

public record CursorPagedResult<T> : ResultBase
{
    public IEnumerable<T> Content { get; init; } = [];

    /// <summary>
    /// 다음 페이지를 가져오기 위한 커서 토큰
    /// </summary>
    public string? NextCursor { get; init; }

    /// <summary>
    /// 다음 데이터 존재 여부
    /// </summary>
    public bool HasMore { get; init; }

    /// <summary>
    /// 전체 개수 (대용량에서는 선택 사항)
    /// </summary>
    public int TotalCount { get; init; }

    public static CursorPagedResult<T> Success(
        IEnumerable<T> items,
        string? nextCursor,
        bool hasMore,
        int totalCount = 0,
        string msg = "목록을 성공적으로 조회했습니다.")
        => new()
        {
            IsSuccess = true,
            Content = items,
            Message = msg,
            Code = RsCode.Ok,
            NextCursor = nextCursor,
            HasMore = hasMore,
            TotalCount = totalCount
        };

    public static CursorPagedResult<T> NotFound(string msg = "요청하신 데이터를 찾을 수 없습니다.")
        => new() { IsSuccess = false, Message = msg, Code = RsCode.NotFound };
}
