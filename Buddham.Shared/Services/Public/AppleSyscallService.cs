using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Interfaces.Public;
using Buddham.Core.Schemas;
using Buddham.Shared.Data;
using Buddham.Shared.Extensions.Public;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Services.Public;

public class AppleSyscallService(
    PublicDbContext context
) : IAppleSyscallService
{
    public async Task<Result<IEnumerable<AppleSyscallSchema.Response>>> GetAsync()
    {
        var data = context.AppleSyscalls.AsNoTracking();

        var res = await data.AsSummary().ToListAsync();

        return Result<IEnumerable<AppleSyscallSchema.Response>>.Success(res, null, "OK");
    }

    public async Task<CursorPagedResult<AppleSyscallSchema.Response>> GetCursorListAsync(CursorPagedQuery query, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<OffsetPagedResult<AppleSyscallSchema.Response>> GetOffsetListAsync(OffsetPagedQuery query, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<CursorPagedResult<AppleSyscallSchema.Response>> GetSemanticListAsync(SemanticSearchQuery query, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<AppleSyscallSchema.Response>> GetByIdAsync(long id)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<AppleSyscallSchema.Response>> GetUserInfoByAdminAsync(long id)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<AppleSyscallSchema.Response>> GetUserInfoAsync(long userId)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> CreateAsync(AppleSyscallSchema.Request request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> UpdateAsync(long id, AppleSyscallSchema.Request request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> RestoreAsync(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteAsync(long id)
    {
        throw new NotImplementedException();
    }
}