using Buddham.Core.Common.Options;
using Buddham.Core.Common.Results;

namespace Buddham.Core.Interfaces.Public;

public interface IVideoProcessorService
{
    /// <summary>
    /// 영상을 임시 저장 후 백그라운드 처리 큐에 등록
    /// </summary>
    Task<VideoProcessResult> SaveAndEnqueueAsync(
        Stream stream,
        string basePath,
        string fileName,
        VideoProcessOptions options,
        CancellationToken ct = default);
}
