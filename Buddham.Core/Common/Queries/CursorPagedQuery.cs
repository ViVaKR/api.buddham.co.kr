namespace Buddham.Core.Common.Queries;

public class CursorPagedQuery
{
    private const int MaxSize = 100;
    public string? Cursor { get; set; }

    public int Size
    {
        get;
        set => field = Math.Clamp(value, 1, MaxSize);
    } = MaxSize;

    public string? SearchKeyword { get; set; } = null; 
    public bool Descending { get; set; } = true; 
}
