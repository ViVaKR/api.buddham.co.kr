using Buddham.Core.EnumTypes;
using Buddham.Core.Models.Public;

namespace Buddham.Shared.Constants;

public static class UploadPolicies
{
    public const long KB = 1024;
    public const long MB = 1024 * KB;
    public const long GB = 1024 * MB;

    public static readonly Dictionary<UploadType, UploadRule> Rules = new()
    {
        [UploadType.Avatar] = new UploadRule
        {
            MaxSize = 2 * MB,
            Extensions = [".jpg", ".jpeg", ".png", ".webp"],
            MimeTypes = ["image/jpeg", "image/png", "image/webp"]
        },
        [UploadType.Image] = new UploadRule
        {
            MaxSize = 10 * MB,
            Extensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"],
            MimeTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"]
        },
        [UploadType.Video] = new UploadRule
        {
            MaxSize = 1 * GB,
            Extensions = [".mp4", ".webm", ".mov"],
            MimeTypes = ["video/mp4", "video/webm", "video/quicktime"]
        },
        [UploadType.Document] = new UploadRule
        {
            MaxSize = 20 * MB,
            Extensions = [".pdf", ".doc", ".docx", ".xls", ".xlsx"],
            MimeTypes =
            [
                "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            ]
        }
    };
}