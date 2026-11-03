namespace Buddham.Core.Common.Queries;

public class SemanticSearchQuery
{
    private const int MaxSize = 100;
    public string? Cursor { get; set; }

    public int Size
    {
        get;
        set => field = Math.Clamp(value, 1, MaxSize);
    } = 20;

    public string? Keyword { get; set; }
    public float Threshold { get; set; } = 0.7f;
}
