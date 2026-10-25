namespace Buddham.Core.Common.Results;

public record OffsetPagedResult<T> : ResultBase
{
    public IEnumerable<T> Content { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalPages => (long)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;

    public static OffsetPagedResult<T> Success(
        IEnumerable<T> content,
        int totalCount,
        int page,
        int pageSize,
        string msg = "목록을 성공적으로 조회했습니다.")
        => new()
        {
            IsSuccess = true,
            Content = content,
            Message = msg,
            Code = RsCode.Ok,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

    public static OffsetPagedResult<T> NotFound(string msg = "요청하신 데이터를 찾을 수 없습니다.")
        => new() { IsSuccess = false, Message = msg, Code = RsCode.NotFound };
}
