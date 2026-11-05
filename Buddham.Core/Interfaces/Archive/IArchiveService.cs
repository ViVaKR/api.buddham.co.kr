using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Archive;

public interface IArchiveService : IBaseService<long, ArchiveSchema.Response, ArchiveSchema.Request>
{
    public record Response { }
    public record Request { }
}
