using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Tangwha;

public interface ITangwhaService : IBaseService<long, TangwhaSchema.Request, TangwhaSchema.Response>,
    IDetailQueryableService<long, GuideSchema.Response>,
    ICursorQueryable<TangwhaSchema.Response>,
    IOffsetQueryable<TangwhaSchema.Response>,
    ISemanticQueryable<TangwhaSchema.Response>
{ }
