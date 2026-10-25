using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Public;

namespace Buddham.Core.Interfaces.Public;

public interface IFfmpegRunner
{
    Task<FfmpegResult> RunAllAsync(VideoJob job, CancellationToken ct = default);
}
