using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Interfaces.Public;
using Buddham.Core.Schemas;
using Buddham.Shared.Extensions;
using Buddham.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Api.Controllers.Public;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class GuideController(
    IGuideService guideService
) : BaseController
{
    /// <summary>
    /// GET LIST ALL (상한: MaxPageSize)
    /// --> Guide/GetListALL
    /// </summary>
    [HttpGet("GetListALL")]
    public async Task<ActionResult<Result<IEnumerable<GuideSchema.Response>>>> GetAllAsync(
        CancellationToken ct)
    {
        var result = await guideService.GetAsync(ct);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// GET Semantic List
    /// --> Guide/GetSemanticList
    /// </summary>
    [HttpGet("GetSemanticList")]
    public async Task<ActionResult<CursorPagedResult<GuideSchema.Response>>> GetSemanticList(
        [FromQuery] SemanticSearchQuery query,
        CancellationToken ct)
    {
        var result = await guideService.GetSemanticListAsync(query, ct);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// GET Cursor Paged List
    /// --> Guide/GetCursorPagedList
    /// </summary>
    [HttpGet("GetCursorPagedList")]
    public async Task<ActionResult<CursorPagedResult<GuideSchema.Response>>> GetCursorPagedList(
        [FromQuery] CursorPagedQuery query,
        CancellationToken ct)
    {
        var result = await guideService.GetCursorListAsync(query, ct);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// GET Offset Paged List
    /// --> Guide/GetOffsetList
    /// </summary>
    [HttpGet("GetOffsetList")]
    public async Task<ActionResult<OffsetPagedResult<GuideSchema.Response>>> GetOffsetList(
        [FromQuery] OffsetPagedQuery query,
        CancellationToken ct)
    {
        var result = await guideService.GetOffsetListAsync(query, ct);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// READ
    /// --> Guide/Read/{id}
    /// </summary>
    [HttpGet("Read/{id:long}")]
    public async Task<ActionResult<Result<GuideSchema.Response>>> ReadAsync(
        long id,
        CancellationToken ct)
    {
        var result = await guideService.GetByIdAsync(id, ct);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// POST, CREATE
    /// --> Guide/Create
    /// </summary>
    [HttpPost("Create")]
    public async Task<ActionResult<Result>> CreateAsync(
        [FromBody] GuideSchema.Request request,
        CancellationToken ct)
    {
        var result = await guideService.CreateAsync(request, ct);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// PUT, UPDATE
    /// --> Guide/Update/{id}
    /// </summary>
    [HttpPut("Update/{id:long}")]
    public async Task<ActionResult<Result>> UpdateAsync(
        long id,
        [FromBody] GuideSchema.Request request,
        CancellationToken ct)
    {
        var result = await guideService.UpdateAsync(id, request, ct);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// DELETE (소프트 삭제)
    /// --> Guide/Delete/{id}
    /// </summary>
    [HttpDelete("Delete/{id:long}")]
    public async Task<ActionResult<Result>> DeleteAsync(
        long id,
        CancellationToken ct)
    {
        var result = await guideService.DeleteAsync(id, ct);
        return result.ToActionResult(this);
    }
}
