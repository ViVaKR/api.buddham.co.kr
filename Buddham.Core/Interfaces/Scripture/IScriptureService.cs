using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Scripture;

public interface IScriptureService : IBaseService<long, ScriptureSchema.Response, ScriptureSchema.Request>
{
}
