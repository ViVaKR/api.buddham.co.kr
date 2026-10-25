using Buddham.Core.Common.Options;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Public;
using Buddham.Core.Interfaces.Public;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Services.Public;

public class VideoProcessorService(
    IVideoJobQueue jobQueue,
    ILogger<VideoProcessorService> logger
) : IVideoProcessorService
{
    public async Task<VideoProcessResult> SaveAndEnqueueAsync(
        Stream stream,
        string basePath,
        string fileName,
        VideoProcessOptions options,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(basePath);
        var rawPath = Path.Combine(basePath, $"{fileName}_raw.mp4");

        await using (var fs = new FileStream(
                         rawPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 81920, // 80KB 버퍼 - 대용량
                         useAsync: true))
        {
            await stream.CopyToAsync(fs, 81920, ct);
        }

        // 2️⃣ 백그라운드 큐에 등록 후 즉시 반환
        var jobId = Guid.NewGuid().ToString("N");
        await jobQueue.EnqueueAsync(new VideoJob
        {
            JobId = jobId,
            FileName = fileName,
            RawPath = rawPath,
            BasePath = basePath,
            Options = options
        }, ct);

        logger.LogInformation("Video enqueued: {JobId} / {FileName}", jobId, fileName);

        return new VideoProcessResult
        {
            RawPath = rawPath,
            JobId = jobId
        };
    }
}
