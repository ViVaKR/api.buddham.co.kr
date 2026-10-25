namespace Buddham.Core.Models.Public;

public class UploadRule
{
    public long MaxSize { get; init; }
    public string[] Extensions { get; init; } = [];
    public string[] MimeTypes { get; init; } = [];
}
