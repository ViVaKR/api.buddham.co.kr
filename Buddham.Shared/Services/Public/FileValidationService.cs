using Buddham.Core.Interfaces.Public;

namespace Buddham.Shared.Services.Public;

public class FileValidationService : IFileValidationService
{
    // 확장자 ↔ ContentType 매핑으로 교차검증
    private static readonly Dictionary<string, string> AllowedMimeMap
        = new(StringComparer.OrdinalIgnoreCase)
        {
            // 이미지
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".webp", "image/webp" },
            { ".gif", "image/gif" },
            { ".svg", "image/svg+xml" },
            { ".bmp", "image/bmp" },
            { ".avif", "image/avif" },
            { ".ico", "image/x-icon" },
            { ".tiff", "image/tiff" },
            // 문서
            { ".pdf", "application/pdf" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".xls", "application/vnd.ms-excel" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".doc", "application/msword" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".txt", "text/plain" },
            { ".csv", "text/csv" },
            { ".json", "application/json" },
            { ".xml", "application/xml" },
            // 압축
            { ".zip", "application/zip" },
            { ".7z", "application/x-7z-compressed" },
            { ".tar", "application/x-tar" },
            { ".gz", "application/gzip" },
            { ".rar", "application/vnd.rar" },
        };

    // 아바타 전용 허용 확장자
    private static readonly HashSet<string> AvatarExtensions
        = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif"
        };

    // ✅ 확장자 + ContentType 교차검증
    public bool IsValidFile(string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName);
        return AllowedMimeMap.TryGetValue(ext, out var expected)
               && expected.Equals(contentType, StringComparison.OrdinalIgnoreCase);
    }

    // ✅ 아바타는 이미지만 + 교차검증
    public bool IsValidAvatarFile(string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName);
        return AvatarExtensions.Contains(ext)
               && AllowedMimeMap.TryGetValue(ext, out var expected)
               && expected.Equals(contentType, StringComparison.OrdinalIgnoreCase);
    }

    // ✅ 이유 명확하게
    public string GetInvalidReason(string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName);

        if (!AllowedMimeMap.TryGetValue(ext, out var expected))
            return $"허용되지 않은 확장자입니다: {ext}";

        if (!expected.Equals(contentType, StringComparison.OrdinalIgnoreCase))
            return $"확장자({ext})와 MIME 타입({contentType})이 일치하지 않습니다.";

        return string.Empty;
    }
}