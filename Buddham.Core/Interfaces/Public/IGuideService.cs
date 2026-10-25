using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Public;

public interface IGuideService : IBaseService<long, GuideSchema.Request, GuideSchema.Response>,
    IDetailQueryableService<long, GuideSchema.Response>,
    ICursorQueryable<GuideSchema.Response>,
    IOffsetQueryable<GuideSchema.Response>,
    ISemanticQueryable<GuideSchema.Response> { }
