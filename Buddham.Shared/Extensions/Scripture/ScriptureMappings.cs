using Buddham.Core.Entities.Scriptures;
using Buddham.Core.EnumTypes;
using S = Buddham.Core.Schemas.ScriptureSchema;

namespace Buddham.Shared.Extensions.Scripture;

/// <summary>
/// 엔터티 → ScriptureSchema.Response 프로젝션. GuideService 의 AsSummary 와 같은 방식(IQueryable 에서 바로 SELECT).
///
/// 요청(Request) → 엔터티 방향은 여기에 두지 않는다. Guide 와 달리 경전은 입력마다 정규화·검증
/// (ScriptureText, 소유·순환·층위 규칙)이 필요해서 각 서비스의 Apply* 메서드가 맡는다.
/// 쓰기 직후 응답도 같은 프로젝션으로 다시 읽어 항상 조회 결과와 같은 모양을 보장한다.
/// </summary>
public static class ScriptureMappings
{
    // ───────────────────────── 계층 ─────────────────────────

    public static IQueryable<S.Tradition.Response> AsResponse(this IQueryable<CanonTradition> q)
        => q.Select(e => new S.Tradition.Response
        {
            Id = e.Id, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
            ParentId = e.ParentId, Code = e.Code, NameKo = e.NameKo,
            NameOriginal = e.NameOriginal, NameRoman = e.NameRoman, Description = e.Description,
        });

    public static IQueryable<S.Canon.Response> AsResponse(this IQueryable<Canon> q)
        => q.Select(e => new S.Canon.Response
        {
            Id = e.Id, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
            TraditionId = e.TraditionId, Code = e.Code, NameKo = e.NameKo,
            NameOriginal = e.NameOriginal, NameRoman = e.NameRoman,
            PrimaryLanguage = e.PrimaryLanguage, Description = e.Description, SortOrder = e.SortOrder,
        });

    public static IQueryable<S.Division.Response> AsResponse(this IQueryable<CanonDivision> q)
        => q.Select(e => new S.Division.Response
        {
            Id = e.Id, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
            CanonId = e.CanonId, ParentId = e.ParentId, Kind = e.Kind, Code = e.Code, NameKo = e.NameKo,
            NameOriginal = e.NameOriginal, NameRoman = e.NameRoman, SortOrder = e.SortOrder,
        });

    public static IQueryable<S.Collection.Response> AsResponse(this IQueryable<CanonCollection> q)
        => q.Select(e => new S.Collection.Response
        {
            Id = e.Id, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
            DivisionId = e.DivisionId, Kind = e.Kind, Code = e.Code, NameKo = e.NameKo,
            NameOriginal = e.NameOriginal, NameRoman = e.NameRoman,
            TraditionId = e.TraditionId, TraditionCertainty = e.TraditionCertainty, TraditionNote = e.TraditionNote,
            SortOrder = e.SortOrder,
        });

    public static IQueryable<S.Group.Response> AsResponse(this IQueryable<CanonGroup> q)
        => q.Select(e => new S.Group.Response
        {
            Id = e.Id, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
            CollectionId = e.CollectionId, ParentId = e.ParentId, Kind = e.Kind, Code = e.Code, NameKo = e.NameKo,
            NameOriginal = e.NameOriginal, NameRoman = e.NameRoman, SortOrder = e.SortOrder,
        });

    // ───────────────────────── 부속 ─────────────────────────

    public static IQueryable<S.Speaker.Response> AsResponse(this IQueryable<CanonSpeaker> q)
        => q.Select(e => new S.Speaker.Response
        {
            Id = e.Id, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
            Code = e.Code, NameKo = e.NameKo, NameOriginal = e.NameOriginal, NameRoman = e.NameRoman,
            Description = e.Description,
        });

    public static IQueryable<S.Source.Response> AsResponse(this IQueryable<CanonSource> q)
        => q.Select(e => new S.Source.Response
        {
            Id = e.Id, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
            Code = e.Code, Name = e.Name, Publisher = e.Publisher, SourceUrl = e.SourceUrl,
            LicenseName = e.LicenseName, LicenseUrl = e.LicenseUrl, Version = e.Version, Note = e.Note,
        });

    // ───────────────────────── 경·식별자·판본 ─────────────────────────

    /// <summary>목록·요약. PrimaryIdentifier 는 "SN 12.2" 처럼 체계와 번호를 붙여 보여 준다.</summary>
    public static IQueryable<S.Work.Response> AsResponse(this IQueryable<CanonWork> q)
        => q.Select(e => new S.Work.Response
        {
            Id = e.Id, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
            CollectionId = e.CollectionId, GroupId = e.GroupId, Code = e.Code,
            TitleKo = e.TitleKo, TitleOriginal = e.TitleOriginal, TitleRoman = e.TitleRoman,
            Kind = e.Kind, Layer = e.Layer, CommentaryOfWorkId = e.CommentaryOfWorkId,
            SpeakerId = e.SpeakerId, SortOrder = e.SortOrder,
            PrimaryIdentifier = e.Identifiers
                .Where(i => i.IsPrimary && i.DeletedAt == null)
                .Select(i => i.Scheme + " " + i.Value)
                .FirstOrDefault(),
        });

    /// <summary>상세: 요약 + 귀속·요약문 + 식별자 + 판본. 한 번의 쿼리로 읽는다.</summary>
    public static IQueryable<S.Work.Detail> AsDetail(this IQueryable<CanonWork> q, bool includeUnpublishedEditions)
        => q.Select(e => new S.Work.Detail
        {
            Id = e.Id, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt,
            CollectionId = e.CollectionId, GroupId = e.GroupId, Code = e.Code,
            TitleKo = e.TitleKo, TitleOriginal = e.TitleOriginal, TitleRoman = e.TitleRoman,
            Kind = e.Kind, Layer = e.Layer, CommentaryOfWorkId = e.CommentaryOfWorkId,
            SpeakerId = e.SpeakerId, SortOrder = e.SortOrder,
            PrimaryIdentifier = e.Identifiers
                .Where(i => i.IsPrimary && i.DeletedAt == null)
                .Select(i => i.Scheme + " " + i.Value)
                .FirstOrDefault(),
            Attribution = e.Attribution,
            AttributionNote = e.AttributionNote,
            Summary = e.Summary,
            Identifiers = e.Identifiers
                .Where(i => i.DeletedAt == null)
                .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.Id)
                .Select(i => new S.Identifier.Response
                {
                    Id = i.Id, WorkId = i.WorkId, Scheme = i.Scheme, Value = i.Value, IsPrimary = i.IsPrimary,
                })
                .ToList(),
            Editions = e.Editions
                .Where(d => d.DeletedAt == null && (includeUnpublishedEditions || d.Status == EditionStatus.Published))
                .OrderBy(d => d.LanguageCode).ThenBy(d => d.Kind).ThenBy(d => d.Id)
                .Select(d => new S.Edition.Response
                {
                    Id = d.Id, CreatedAt = d.CreatedAt, UpdatedAt = d.UpdatedAt,
                    WorkId = d.WorkId, SourceId = d.SourceId, Kind = d.Kind, Status = d.Status,
                    LanguageCode = d.LanguageCode, Script = d.Script, TranslatorName = d.TranslatorName,
                    BasedOnEditionId = d.BasedOnEditionId, Revision = d.Revision,
                    ContentHash = d.ContentHash, SegmentCount = d.SegmentCount,
                })
                .ToList(),
        });

    public static IQueryable<S.Identifier.Response> AsResponse(this IQueryable<CanonWorkIdentifier> q)
        => q.Select(i => new S.Identifier.Response
        {
            Id = i.Id, WorkId = i.WorkId, Scheme = i.Scheme, Value = i.Value, IsPrimary = i.IsPrimary,
        });

    public static IQueryable<S.Edition.Response> AsResponse(this IQueryable<CanonEdition> q)
        => q.Select(d => new S.Edition.Response
        {
            Id = d.Id, CreatedAt = d.CreatedAt, UpdatedAt = d.UpdatedAt,
            WorkId = d.WorkId, SourceId = d.SourceId, Kind = d.Kind, Status = d.Status,
            LanguageCode = d.LanguageCode, Script = d.Script, TranslatorName = d.TranslatorName,
            BasedOnEditionId = d.BasedOnEditionId, Revision = d.Revision,
            ContentHash = d.ContentHash, SegmentCount = d.SegmentCount,
        });

    // ───────────────────────── 원문·관계·주제·사경 (다음 단계에서 사용) ─────────────────────────

    /// <summary>본문 응답. 임베딩 벡터와 해시는 싣지 않는다.</summary>
    public static IQueryable<S.Segment.Response> AsResponse(this IQueryable<CanonSegment> q)
        => q.Select(s => new S.Segment.Response
        {
            Id = s.Id, EditionId = s.EditionId, Ref = s.SegmentRef, SortOrder = s.SortOrder,
            Kind = s.Kind, PageRef = s.PageRef, Text = s.Text,
        });

    public static IQueryable<S.Parallel.Response> AsResponse(this IQueryable<CanonParallel> q)
        => q.Select(p => new S.Parallel.Response
        {
            Id = p.Id, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt,
            FromWorkId = p.FromWorkId, ToWorkId = p.ToWorkId,
            FromSegmentId = p.FromSegmentId, ToSegmentId = p.ToSegmentId, Kind = p.Kind, Note = p.Note,
        });

    public static IQueryable<S.Topic.Response> AsResponse(this IQueryable<DhammaTopic> q)
        => q.Select(t => new S.Topic.Response
        {
            Id = t.Id, CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt,
            ParentId = t.ParentId, Code = t.Code, NameKo = t.NameKo,
            NamePali = t.NamePali, NameSanskrit = t.NameSanskrit, Description = t.Description, SortOrder = t.SortOrder,
        });

    public static IQueryable<S.Tag.Response> AsResponse(this IQueryable<CanonSegmentTopic> q)
        => q.Select(t => new S.Tag.Response
        {
            Id = t.Id, CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt,
            SegmentId = t.SegmentId, TopicId = t.TopicId, Source = t.Source, Confidence = t.Confidence, Note = t.Note,
        });

    public static IQueryable<S.Lekhana.Session> AsResponse(this IQueryable<LekhanaSession> q)
        => q.Select(s => new S.Lekhana.Session
        {
            Id = s.Id, CreatedAt = s.CreatedAt, UpdatedAt = s.UpdatedAt,
            EditionId = s.EditionId, Title = s.Title, Vow = s.Vow, Status = s.Status,
            StartedAt = s.StartedAt, CompletedAt = s.CompletedAt,
        });
}
