using System.Linq.Expressions;
using Buddham.Core.Common.Options;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Public;
using Buddham.Core.Interfaces.Common;
using Buddham.Core.Interfaces.Public;
using Buddham.Core.Schemas;
using Buddham.Shared.Caching;
using Buddham.Shared.Constants;
using Buddham.Shared.Data;
using Buddham.Shared.Extensions.Public;
using Buddham.Shared.Services.Crud;

namespace Buddham.Shared.Services.Public;

public class GuideService(
    PublicDbContext context,
    CrudDependencies deps,
    IEmbeddingService embeddingService
) : EmbeddingCrudService<Guide, GuideSchema.Request, GuideSchema.Response>(
        context,
        deps,
        embeddingService,
        GuideCacheOptions),
    IGuideService
{
    private static readonly EntityCacheOptions GuideCacheOptions =
        EntityCacheOptionsFactory.For<Guid>("법문 가이드", version: 2);
    private static readonly SortMap<Guide> GuideSorts
        = new SortMap<Guide>().Add("id", x => x.Id).Add("title", x => x.Title);
    private static readonly Expression<Func<Guide, string?>>[] GuideSearchFields = [x => x.Title, x => x.Content];

    // ── 엔티티마다 다른 부분은 이게 전부 ──────────────────────

    protected override IQueryable<GuideSchema.Response> Project(IQueryable<Guide> query)
        => query.AsSummary();

    protected override Guide ToEntity(GuideSchema.Request request, Guid userId, DateTime now)
        => request.ToEntity(userId, now);

    protected override void ApplyUpdate(Guide entity, GuideSchema.Request request, DateTime now)
        => entity.ApplyUpdate(request, now);   // 임베딩은 베이스가 처리하므로 건드리지 않는다

    protected override SortMap<Guide> Sorts => GuideSorts;
    protected override IReadOnlyList<Expression<Func<Guide, string?>>> SearchFields => GuideSearchFields;

    protected override Result? Validate(GuideSchema.Request request) => new Validator()
            .Text("title", request.Title, "제목", AppInfrastructure.MaxTitleLength)
            .Text("content", request.Content, "본문", AppInfrastructure.MaxContentLength, trimForLength: false)
            .EnumDefined("guideCategory", request.GuideCategory, "카테고리")
            .ToResult();

    protected override string EmbeddingText(GuideSchema.Request request)
        => BuildEmbeddingText(request.Title, request.Content);

    protected override string EmbeddingText(Guide entity)
        => BuildEmbeddingText(entity.Title, entity.Content);
}
