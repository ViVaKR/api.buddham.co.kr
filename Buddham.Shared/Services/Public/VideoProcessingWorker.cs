using Buddham.Core.Entities.Public;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Public;
using Buddham.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Services.Public;

public class VideoProcessingWorker(
    IVideoJobQueue jobQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<VideoProcessingWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("VideoProcessingWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            VideoJob job;
            try
            {
                job = await jobQueue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ProcessJobAsync(job, stoppingToken);
        }

        logger.LogInformation("VideoProcessingWorker stopped");
    }

    private async IAsyncEnumerable<bool> ReadJobsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            VideoJob job;
            try
            {
                job = await jobQueue.DequeueAsync(ct);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            await ProcessJobAsync(job, ct);
            yield return true;
        }
    }

    private async Task ProcessJobAsync(VideoJob job, CancellationToken ct)
    {
        logger.LogInformation("Processing video job: {JobId}", job.JobId);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PublicDbContext>();
        var ffmpeg = scope.ServiceProvider.GetRequiredService<IFfmpegRunner>();

        var entity = await db.FileDatas
            .FirstOrDefaultAsync(f => f.StoredFileName == job.FileName, ct);

        if (entity is null)
        {
            logger.LogWarning("FileData not found for job: {JobId}", job.JobId);
            return;
        }

        try
        {
            var results = await ffmpeg.RunAllAsync(job, ct);

            entity.Status = FileStatus.Completed;
            entity.ThumbnailPath = results.ThumbnailRelPath;
            entity.WebpPath = null; // Video는 WebP 불필요
            // HLS 경로 등 추가 컬럼이 있다면 여기서 설정
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FFmpeg failed for job: {JobId}", job.JobId);

            entity.Status = FileStatus.Failed;
            CleanupFiles(job);
        }
        finally
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private void CleanupFiles(VideoJob job)
    {
        var patterns = new[]
        {
            $"{job.FileName}_raw.mp4",
            $"{job.FileName}.mp4",
            $"{job.FileName}_thumb.jpg",
            $"{job.FileName}_hls" // HLS 디렉터리
        };

        foreach (var name in patterns)
        {
            var fullPath = Path.Combine(job.BasePath, name);

            if (Directory.Exists(fullPath))
                Directory.Delete(fullPath, recursive: true);
            else if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
