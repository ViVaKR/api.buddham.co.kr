using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Interfaces.Tangwha;
using Buddham.Core.Schemas;

namespace Buddham.Shared.Services.Tangwha;

public class TangwhaService : ITangwhaService
{
    public Task<Result<IEnumerable<TangwhaSchema.Response>>> GetAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Result> CreateAsync(TangwhaSchema.Request request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Result> DeleteAsync(long id)
    {
        throw new NotImplementedException();
    }


    public Task<Result<GuideSchema.Response>> GetByIdAsync(long id)
    {
        throw new NotImplementedException();
    }

    public Task<CursorPagedResult<TangwhaSchema.Response>> GetCursorListAsync(CursorPagedQuery query, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<OffsetPagedResult<TangwhaSchema.Response>> GetOffsetListAsync(OffsetPagedQuery query, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<CursorPagedResult<TangwhaSchema.Response>> GetSemanticListAsync(SemanticSearchQuery query, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Result> UpdateAsync(long id, TangwhaSchema.Request request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
