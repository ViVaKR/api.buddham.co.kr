using Buddham.Core.Common.Primitives;
using Buddham.Core.EnumTypes;

namespace Buddham.Core.Entities.Public;

public class FileData : EntityBase
{
    public required Guid UserId { get; set; }
    public string StoredFileName { get; set; } = null!;
    public required string Path { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? WebpPath { get; set; }
    public long? Size { get; set; }
    public required string OriginalFilename { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public bool? IsShared { get; set; } = false;
    public UploadType FileType { get; set; }
    public FileStatus Status { get; set; } = FileStatus.Pending;
}