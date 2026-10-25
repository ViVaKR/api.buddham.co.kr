using Buddham.Core.Common.Options;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Entities.Public;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Public;
using Buddham.Shared.Constants;
using Buddham.Shared.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Services.Public;

public class FileManagerService(
    PublicDbContext context,
    UserManager<ApplicationUser> userManager,
    IImageProcessorService imageProcessorService,
    IVideoProcessorService videoProcessorService,
    IWebHostEnvironment env,
    ILogger<FileManagerService> logger
) : IFileManagerService
{
    public async Task<Result<FileData>> UploadAvatarAsync(Stream fileStream,
        string fileName,
        string contentType,
        Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        var storedName = FileName.Generate();
        var basePath = Path.Combine(env.ContentRootPath, "Storage", "Avatars", userId.ToString());
        var oldRecords = await context.FileDatas
            .Where(x => x.UserId == userId && x.FileType == UploadType.Avatar)
            .ToListAsync();

        foreach (var old in oldRecords)
        {
            try
            {
                DeletePhysicalFiles(basePath, old.StoredFileName);
            }
            catch (Exception ex)
            {
                logger.LogWarning("⚠️ 기존 아바타 폐기 실패: {Error}", ex.Message);
            }
        }

        Directory.CreateDirectory(basePath);
        context.FileDatas.RemoveRange(oldRecords);

        // 📝 순수 메타데이터로 엔티티 생성 (fileStream.Length 사용!)
        var entity = new FileData
        {
            UserId = userId,
            OriginalFilename = fileName, // 매개변수로 받은 원본 이름
            StoredFileName = storedName,
            Path = $"/Avatars/{userId}",
            Size = fileStream.Length, // 🌟 스트림의 길이를 다이렉트로 측정!
            ContentType = contentType, // 🌟 매개변수로 받은 타입 꽂기!
            FileType = UploadType.Avatar,
            Status = FileStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        context.FileDatas.Add(entity);

        try
        {
            await imageProcessorService.ProcessAsync(fileStream, basePath, storedName);

            // 🏆 가공 완료 후 영토 확정
            entity.Status = FileStatus.Completed;
            entity.ThumbnailPath = $"/Avatars/{userId}/{storedName}_thumb.jpg";
            entity.WebpPath = $"/Avatars/{userId}/{storedName}.webp";

            await context.SaveChangesAsync();

            if (user is null) return Result<FileData>.Success(entity);
            user.Avatar = entity.WebpPath;
            await userManager.UpdateAsync(user);
            return Result<FileData>.Success(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ 아바ta 처리 중 결계 파괴: {UserId}", userId);
            DeletePhysicalFiles(basePath, storedName);
            entity.Status = FileStatus.Failed;
            await context.SaveChangesAsync();
            return Result.Error($"Failed: {ex.Message}");
        }
    }

    public async Task<Result<FileData>> UploadImageAsync(Stream fileStream, string fileName, string contentType, Guid userId)
    {
        var storedName = FileName.Generate();

        var basePath = Path.Combine(ApiRoutes.Statics.Root, "Images");

        var entity = new FileData
        {
            UserId = userId,
            OriginalFilename = fileName,
            StoredFileName = storedName,
            Path = ApiRoutes.Files.Images,
            Size = fileStream.Length,
            ContentType = contentType,
            FileType = UploadType.Image,
            Status = FileStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        context.FileDatas.Add(entity);

        await context.SaveChangesAsync();

        try
        {
            await imageProcessorService.ProcessAsync(fileStream, basePath, fileName);

            // 👉 성공 시 상태 변경
            entity.Status = FileStatus.Completed;
            entity.ThumbnailPath = $"{basePath}/{fileName}_thumb.jpg";
            entity.WebpPath = $"{basePath}/{fileName}.webp";

            await context.SaveChangesAsync();

            return Result<FileData>.Success(entity, "OK");
        }
        catch (Exception)
        {
            // 👉 실패 시 파일 삭제 (보상 트랜잭션)
            DeletePhysicalFiles(basePath, fileName);
            entity.Status = FileStatus.Failed;
            await context.SaveChangesAsync();
            throw;
        }
    }


    public async Task<Result<FileData>> UploadVideoAsync(Stream fileStream, string fileName, string contentType, Guid userId)
    {
        var storedName = FileName.Generate();
        var basePath = Path.Combine(ApiRoutes.Statics.Root, "Videos");

        if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

        var entity = new FileData
        {
            UserId = userId,
            OriginalFilename = fileName,
            StoredFileName = storedName,
            Path = ApiRoutes.Files.Videos,
            Size = fileStream.Length, // 🌟 스트림 길이 측정!
            ContentType = contentType,
            FileType = UploadType.Video,
            Status = FileStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            // (주의: 구버전의 대소문자 API가 아닌, 새로 바뀐 정갈한 데이터베이스 구조를 따름!)
        };

        context.FileDatas.Add(entity);
        await context.SaveChangesAsync();

        try
        {
            await videoProcessorService.SaveAndEnqueueAsync(
                fileStream,
                basePath,
                storedName,
                new VideoProcessOptions
                {
                    GenerateThumbnail = true,
                    TranscodeToMp4 = true,
                    TranscodeToHls = true,
                    ThumbnailAtSecond = 3,
                    Quality = VideoQuality.High
                });

            entity.Status = FileStatus.Processing;
            await context.SaveChangesAsync();

            return Result<FileData>.Success(entity, "Video uploaded. Processing in background.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "🎥 비디오 비동기 큐잉 실패: {UserId}", userId);
            entity.Status = FileStatus.Failed;
            await context.SaveChangesAsync();
            throw;
        }
    }

    // TODO : 구현 - File, Audio, Document
    public async Task<Result> UploadFileAsync(Stream fileStream, string fileName, string contentType, Guid userId) => Result.Success();
    public async Task<Result> UploadAudioAsync(Stream fileStream, string fileName, string contentType, Guid userId) => Result.Success();
    public async Task<Result> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, Guid userId) => Result.Success();

    private static void DeletePhysicalFiles(string basePath, string fileName)
    {
        var files = new[]
        {
            $"{fileName}.jpg",
            $"{fileName}_thumb.jpg",
            $"{fileName}.webp",
            $"{fileName}_orig.jpg"
        };

        foreach (var f in files)
        {
            var full = Path.Combine(basePath, f);
            // 로그나 디버그 창에서 실제 경로 확인
            Console.WriteLine($"DEBUG: 삭제 경로 => {basePath}");
            if (File.Exists(full))
                File.Delete(full);
        }
    }
}