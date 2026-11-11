using Buddham.Core.Common.Results;
using Buddham.Core.Schemas;

namespace Buddham.Core.Interfaces.Scripture;

/// <summary>발화자(Speaker)와 출전(Source) — 규칙이 얇은 참조 데이터.</summary>
public interface ICatalogService
{
    Task<Result<IReadOnlyList<ScriptureSchema.Speaker.Response>>> GetSpeakersAsync(CancellationToken ct);
    Task<Result<ScriptureSchema.Speaker.Response>> CreateSpeakerAsync(ScriptureSchema.Speaker.Request request, CancellationToken ct);
    Task<Result<ScriptureSchema.Speaker.Response>> UpdateSpeakerAsync(long id, ScriptureSchema.Speaker.Request request, CancellationToken ct);
    Task<Result> DeleteSpeakerAsync(long id, CancellationToken ct);

    Task<Result<IReadOnlyList<ScriptureSchema.Source.Response>>> GetSourcesAsync(CancellationToken ct);
    Task<Result<ScriptureSchema.Source.Response>> CreateSourceAsync(ScriptureSchema.Source.Request request, CancellationToken ct);
    Task<Result<ScriptureSchema.Source.Response>> UpdateSourceAsync(long id, ScriptureSchema.Source.Request request, CancellationToken ct);
    Task<Result> DeleteSourceAsync(long id, CancellationToken ct);
}
