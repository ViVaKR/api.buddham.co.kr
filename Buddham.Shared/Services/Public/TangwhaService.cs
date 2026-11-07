using Buddham.Core.Common;
using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Interfaces.Ollama;
using Buddham.Core.Interfaces.Tangwha;
using Buddham.Core.Schemas;
using Buddham.Shared.Data;
using Buddham.Shared.Extensions.Public;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Buddham.Shared.Services.Public;

public class TangwhaService(
    PublicDbContext context,
    IOllamaService ollamaService,
    UserManager<ApplicationUser> userManager) : ITangwhaService
{
    public Task<Result> CreateAsync(TangwhaSchema.Request request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Result> DeleteAsync(long id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<IEnumerable<TangwhaSchema.Response>>> GetAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TangwhaSchema.Response>> GetByIdAsync(long id, CancellationToken ct = default)
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
