namespace Buddham.Core.Common.Queries;

public class OffsetPagedQuery
{
    private const int MaxPageSize = 100;

    public int PageNumber
    {
        get;
        set => field = value < 1 ? 1 : value;
    } = 1;

    public int PageSize
    {
        get;
        set => field = Math.Clamp(value, 1, MaxPageSize);
    } = MaxPageSize;

    public string? SearchKeyword { get; set; }
    public string? SortBy { get; set; }
    public bool Descending { get; set; } = true;
}
