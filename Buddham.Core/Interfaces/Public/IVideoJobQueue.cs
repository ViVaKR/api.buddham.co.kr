using Buddham.Core.Entities.Public;

namespace Buddham.Core.Interfaces.Public;

public interface IVideoJobQueue
{
    ValueTask EnqueueAsync(VideoJob job, CancellationToken ct = default);
    ValueTask<VideoJob> DequeueAsync(CancellationToken ct);
}
