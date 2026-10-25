using System.Threading.Channels;
using Buddham.Core.Entities.Public;
using Buddham.Core.Interfaces.Public;

namespace Buddham.Shared.Services.Public;

public class VideoJobQueue: IVideoJobQueue
{
    // 최대 동시 처리 큐 크기 제한 (서버 부하 방지)
    private readonly Channel<VideoJob> _channel =
        Channel.CreateBounded<VideoJob>(new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(VideoJob job, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(job, ct);

    public ValueTask<VideoJob> DequeueAsync(CancellationToken ct)
        => _channel.Reader.ReadAsync(ct); 
}
