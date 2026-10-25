using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Public;

namespace Buddham.Core.Interfaces.Public;

public interface IFileManagerService
{
    // 🌊 IFormFile 대신 순수한 Stream과 파일 메타데이터만 수용!
    Task<Result<FileData>> UploadImageAsync(Stream fileStream, string fileName, string contentType, Guid userId);
    Task<Result<FileData>> UploadAvatarAsync(Stream fileStream, string fileName, string contentType, Guid userId);
    Task<Result> UploadFileAsync(Stream fileStream, string fileName, string contentType, Guid userId);
    Task<Result> UploadAudioAsync(Stream fileStream, string fileName, string contentType, Guid userId);
    Task<Result> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, Guid userId);
    Task<Result<FileData>> UploadVideoAsync(Stream fileStream, string fileName, string contentType, Guid userId);
}