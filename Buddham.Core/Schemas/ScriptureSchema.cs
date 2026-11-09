using Buddham.Core.EnumTypes;

namespace Buddham.Core.Schemas;

/// <summary>
/// 경전(scripture 스키마) DTO 모음. GuideSchema 와 같은 규약:
/// 엔터티마다 중첩 정적 클래스 안에 Request / Response 를 둔다.
/// - Request 의 Id 는 두지 않는다(수정은 경로의 id 사용). 서버가 정하는 값(UserId, 해시, 임베딩, 상태)은 받지 않는다.
/// - Response 에는 임베딩 벡터를 싣지 않는다.
/// 삭제된 행은 조회 단계에서 걸러지므로 Guide 의 IsDelete 는 두지 않았다.
/// </summary>
public static class ScriptureSchema
{
    /// <summary>모든 Response 의 공통 감사 필드</summary>
    public abstract record ResponseBase
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ═════════════════════ 계층 ═════════════════════

    public static class Tradition
    {
        public record Request
        {
            public long? ParentId { get; set; }
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NameOriginal { get; set; }
            public string? NameRoman { get; set; }
            public string? Description { get; set; }
        }

        public record Response : ResponseBase
        {
            public long? ParentId { get; set; }
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NameOriginal { get; set; }
            public string? NameRoman { get; set; }
            public string? Description { get; set; }
        }
    }

    public static class Canon
    {
        public record Request
        {
            public long? TraditionId { get; set; }
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NameOriginal { get; set; }
            public string? NameRoman { get; set; }
            public string? PrimaryLanguage { get; set; }
            public string? Description { get; set; }
            public int SortOrder { get; set; }
        }

        public record Response : ResponseBase
        {
            public long? TraditionId { get; set; }
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NameOriginal { get; set; }
            public string? NameRoman { get; set; }
            public string? PrimaryLanguage { get; set; }
            public string? Description { get; set; }
            public int SortOrder { get; set; }
        }
    }

    public static class Division
    {
        public record Request
        {
            public required long CanonId { get; set; }
            public long? ParentId { get; set; }
            public required DivisionKind Kind { get; set; }
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NameOriginal { get; set; }
            public string? NameRoman { get; set; }
            public int SortOrder { get; set; }
        }

        public record Response : ResponseBase
        {
            public long CanonId { get; set; }
            public long? ParentId { get; set; }
            public DivisionKind Kind { get; set; }
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NameOriginal { get; set; }
            public string? NameRoman { get; set; }
            public int SortOrder { get; set; }
        }
    }

    public static class Collection
    {
        public record Request
        {
            public required long DivisionId { get; set; }
            public CollectionKind? Kind { get; set; }
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NameOriginal { get; set; }
            public string? NameRoman { get; set; }
            public long? TraditionId { get; set; }
            public CertaintyLevel TraditionCertainty { get; set; } = CertaintyLevel.Unknown;
            public string? TraditionNote { get; set; }
            public int SortOrder { get; set; }
        }

        public record Response : ResponseBase
        {
            public long DivisionId { get; set; }
            public CollectionKind? Kind { get; set; }
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NameOriginal { get; set; }
            public string? NameRoman { get; set; }
            public long? TraditionId { get; set; }
            public CertaintyLevel TraditionCertainty { get; set; }
            public string? TraditionNote { get; set; }
            public int SortOrder { get; set; }
        }
    }

    public static class Group
    {
        public record Request
        {
            public required long CollectionId { get; set; }
            public long? ParentId { get; set; }
            public GroupKind? Kind { get; set; }
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NameOriginal { get; set; }
            public string? NameRoman { get; set; }
            public int SortOrder { get; set; }
        }

        public record Response : ResponseBase
        {
            public long CollectionId { get; set; }
            public long? ParentId { get; set; }
            public GroupKind? Kind { get; set; }
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NameOriginal { get; set; }
            public string? NameRoman { get; set; }
            public int SortOrder { get; set; }
        }
    }

    // ═════════════════════ 부속(발화자·출전) ═════════════════════

    public static class Speaker
    {
        public record Request
        {
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NameOriginal { get; set; }
            public string? NameRoman { get; set; }
            public string? Description { get; set; }
        }

        public record Response : ResponseBase
        {
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NameOriginal { get; set; }
            public string? NameRoman { get; set; }
            public string? Description { get; set; }
        }
    }

    public static class Source
    {
        public record Request
        {
            public required string Code { get; set; }
            public required string Name { get; set; }
            public string? Publisher { get; set; }
            public string? SourceUrl { get; set; }
            public string? LicenseName { get; set; }
            public string? LicenseUrl { get; set; }
            public string? Version { get; set; }
            public string? Note { get; set; }
        }

        public record Response : ResponseBase
        {
            public required string Code { get; set; }
            public required string Name { get; set; }
            public string? Publisher { get; set; }
            public string? SourceUrl { get; set; }
            public string? LicenseName { get; set; }
            public string? LicenseUrl { get; set; }
            public string? Version { get; set; }
            public string? Note { get; set; }
        }
    }

    // ═════════════════════ 경(작품)과 식별자 ═════════════════════

    public static class Identifier
    {
        public record Request
        {
            /// <summary>체계 코드(대문자): SC, PTS, VRI, T, CBETA, KBC ...</summary>
            public required string Scheme { get; set; }
            public required string Value { get; set; }
            public bool IsPrimary { get; set; }
        }

        public record Response
        {
            public long Id { get; set; }
            public long WorkId { get; set; }
            public required string Scheme { get; set; }
            public required string Value { get; set; }
            public bool IsPrimary { get; set; }
        }

        /// <summary>"SN 12.2" 같은 번호로 경을 찾은 결과(체계가 달라 겹칠 수 있어 목록)</summary>
        public record Match
        {
            public long WorkId { get; set; }
            public long CollectionId { get; set; }
            public required string Code { get; set; }
            public required string TitleKo { get; set; }
            public required string Scheme { get; set; }
            public required string Value { get; set; }
        }
    }

    public static class Work
    {
        public record Request
        {
            public required long CollectionId { get; set; }
            public long? GroupId { get; set; }
            public required string Code { get; set; }
            public required string TitleKo { get; set; }
            public string? TitleOriginal { get; set; }
            public string? TitleRoman { get; set; }
            public required WorkKind Kind { get; set; }
            public TextLayer Layer { get; set; } = TextLayer.Root;
            public AttributionKind Attribution { get; set; } = AttributionKind.Unknown;
            public string? AttributionNote { get; set; }
            public long? SpeakerId { get; set; }
            /// <summary>주석 층일 때 해설 대상 원전. 원전 층이면 비워 둔다.</summary>
            public long? CommentaryOfWorkId { get; set; }
            public string? Summary { get; set; }
            public int SortOrder { get; set; }
            /// <summary>생성 시에만 사용. 수정은 식별자 전용 API 를 쓴다.</summary>
            public List<Identifier.Request>? Identifiers { get; set; }
        }

        /// <summary>목록·메뉴용 요약</summary>
        public record Response : ResponseBase
        {
            public long CollectionId { get; set; }
            public long? GroupId { get; set; }
            public required string Code { get; set; }
            public required string TitleKo { get; set; }
            public string? TitleOriginal { get; set; }
            public string? TitleRoman { get; set; }
            public WorkKind Kind { get; set; }
            public TextLayer Layer { get; set; }
            public long? CommentaryOfWorkId { get; set; }
            public long? SpeakerId { get; set; }
            public int SortOrder { get; set; }
            /// <summary>대표 식별자(예: "SN 12.2")</summary>
            public string? PrimaryIdentifier { get; set; }
        }

        /// <summary>단건 상세: 요약 + 귀속·요약문 + 식별자 + 판본 목록</summary>
        public record Detail : Response
        {
            public AttributionKind Attribution { get; set; }
            public string? AttributionNote { get; set; }
            public string? Summary { get; set; }
            public List<Identifier.Response> Identifiers { get; set; } = [];
            public List<Edition.Response> Editions { get; set; } = [];
        }
    }

    // ═════════════════════ 판본과 세그먼트 ═════════════════════

    public static class Edition
    {
        public record Request
        {
            public required long WorkId { get; set; }
            public required string LanguageCode { get; set; }
            public string? Script { get; set; }
            public required EditionKind Kind { get; set; }
            public string? Revision { get; set; }
            public string? TranslatorName { get; set; }
            public long? SourceId { get; set; }
            /// <summary>번역본이면 필수: 저본 판본</summary>
            public long? BasedOnEditionId { get; set; }
        }

        public record Response : ResponseBase
        {
            public long WorkId { get; set; }
            public long? SourceId { get; set; }
            public EditionKind Kind { get; set; }
            public EditionStatus Status { get; set; }
            public required string LanguageCode { get; set; }
            public string? Script { get; set; }
            public string? TranslatorName { get; set; }
            public long? BasedOnEditionId { get; set; }
            public string? Revision { get; set; }
            public string? ContentHash { get; set; }
            public int SegmentCount { get; set; }
        }

        public record StatusRequest
        {
            public required EditionStatus Target { get; set; }
        }

        public record StatusResponse
        {
            public EditionStatus Status { get; set; }
            public int SegmentCount { get; set; }
            public string? ContentHash { get; set; }
            /// <summary>공개를 막지는 않지만 확인이 필요한 점(저본과 정렬 불일치 등)</summary>
            public List<string> Warnings { get; set; } = [];
        }
    }

    public static class Segment
    {
        public record Response
        {
            public long Id { get; set; }
            public long EditionId { get; set; }
            public required string Ref { get; set; }
            public int SortOrder { get; set; }
            public SegmentKind Kind { get; set; }
            public string? PageRef { get; set; }
            public required string Text { get; set; }
        }

        /// <summary>가져오기 한 줄. 입력 목록의 순서가 곧 읽는 순서(SortOrder)가 된다.</summary>
        public record ImportItem
        {
            public required string Ref { get; set; }
            public required string Text { get; set; }
            public SegmentKind Kind { get; set; } = SegmentKind.Prose;
            public string? PageRef { get; set; }
        }

        public record ImportRequest
        {
            public required List<ImportItem> Items { get; set; }
            /// <summary>입력에 없는 기존 세그먼트를 지울지(참조 중이면 전체 거부)</summary>
            public bool RemoveMissing { get; set; }
        }

        public record ImportResponse
        {
            public int Added { get; set; }
            public int Updated { get; set; }
            public int Unchanged { get; set; }
            public int Removed { get; set; }
            public int EmbeddingsInvalidated { get; set; }
        }

        /// <summary>같은 SegmentRef 로 맞춘 대역(원문·번역) 한 줄</summary>
        public record AlignedRow
        {
            public required string Ref { get; set; }
            public int SortOrder { get; set; }
            /// <summary>판본 Id → 본문</summary>
            public required Dictionary<long, string> TextsByEdition { get; set; }
        }
    }

    // ═════════════════════ 검색 ═════════════════════

    public static class Search
    {
        public enum Mode { Text, Semantic, Hybrid }

        public record Request
        {
            public required string Query { get; set; }
            public Mode Mode { get; set; } = Mode.Hybrid;
            public string? LanguageCode { get; set; }
            public long? CollectionId { get; set; }
            public long? WorkId { get; set; }
            public int Take { get; set; } = 20;
        }

        public record Hit
        {
            public long SegmentId { get; set; }
            public required string Ref { get; set; }
            public required string Text { get; set; }
            public long EditionId { get; set; }
            public required string LanguageCode { get; set; }
            public long WorkId { get; set; }
            public required string WorkTitleKo { get; set; }
            public double Score { get; set; }
        }
    }

    // ═════════════════════ 병행경 ═════════════════════

    public static class Parallel
    {
        public record Request
        {
            public required long FromWorkId { get; set; }
            public required long ToWorkId { get; set; }
            public ParallelKind Kind { get; set; } = ParallelKind.Full;
            /// <summary>작품 단위면 둘 다 비우고, 구절 단위면 둘 다 채운다.</summary>
            public long? FromSegmentId { get; set; }
            public long? ToSegmentId { get; set; }
            public string? Note { get; set; }
        }

        public record Response : ResponseBase
        {
            public long FromWorkId { get; set; }
            public long ToWorkId { get; set; }
            public long? FromSegmentId { get; set; }
            public long? ToSegmentId { get; set; }
            public ParallelKind Kind { get; set; }
            public string? Note { get; set; }
        }

        /// <summary>특정 경(또는 구절) 기준으로 방향을 정리한 보기: 항상 "상대"가 앞에 온다.</summary>
        public record View
        {
            public long Id { get; set; }
            public bool Outbound { get; set; }
            public long OtherWorkId { get; set; }
            public required string OtherWorkCode { get; set; }
            public required string OtherWorkTitleKo { get; set; }
            public ParallelKind Kind { get; set; }
            public long? ThisSegmentId { get; set; }
            public long? OtherSegmentId { get; set; }
            public string? Note { get; set; }
        }
    }

    // ═════════════════════ 교리 주제 ═════════════════════

    public static class Topic
    {
        public record Request
        {
            public long? ParentId { get; set; }
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NamePali { get; set; }
            public string? NameSanskrit { get; set; }
            public string? Description { get; set; }
            public int SortOrder { get; set; }
        }

        public record Response : ResponseBase
        {
            public long? ParentId { get; set; }
            public required string Code { get; set; }
            public required string NameKo { get; set; }
            public string? NamePali { get; set; }
            public string? NameSanskrit { get; set; }
            public string? Description { get; set; }
            public int SortOrder { get; set; }
        }
    }

    public static class Tag
    {
        public record Request
        {
            public required long SegmentId { get; set; }
            public required long TopicId { get; set; }
            public TagSource Source { get; set; } = TagSource.Manual;
            /// <summary>AI 태그일 때만 0~1. 사람이 단 태그면 무시된다.</summary>
            public double? Confidence { get; set; }
            public string? Note { get; set; }
        }

        public record Response : ResponseBase
        {
            public long SegmentId { get; set; }
            public long TopicId { get; set; }
            public TagSource Source { get; set; }
            public double? Confidence { get; set; }
            public string? Note { get; set; }
        }
    }

    // ═════════════════════ 사경 ═════════════════════
    // UserId 는 Request 에 두지 않는다. 컨트롤러가 BaseController.UserId 를 서비스에 넘긴다.

    public static class Lekhana
    {
        public record StartRequest
        {
            public required long EditionId { get; set; }
            public string? Title { get; set; }
            /// <summary>발원문</summary>
            public string? Vow { get; set; }
        }

        public record Session : ResponseBase
        {
            public long EditionId { get; set; }
            public string? Title { get; set; }
            public string? Vow { get; set; }
            public LekhanaStatus Status { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime? CompletedAt { get; set; }
        }

        public record StartResponse
        {
            public required Session Session { get; set; }
            /// <summary>같은 판본의 진행 중 세션이 있어 새로 만들지 않고 이어 쓰게 한 경우 true</summary>
            public bool Resumed { get; set; }
        }

        public record Progress
        {
            public long SessionId { get; set; }
            public LekhanaStatus Status { get; set; }
            public int Total { get; set; }
            public int Copied { get; set; }
            public double Percent { get; set; }
            public long? NextSegmentId { get; set; }
        }

        public record SubmitRequest
        {
            public required long SegmentId { get; set; }
            public string? TypedText { get; set; }
            public string? ImageUrl { get; set; }
        }

        public record SubmitResponse
        {
            public long EntryId { get; set; }
            /// <summary>글자 단위 일치율 0~1. 글을 입력하지 않고 이미지만 올렸으면 null.</summary>
            public double? Accuracy { get; set; }
            public required Progress Progress { get; set; }
        }

        public record Entry
        {
            public long EntryId { get; set; }
            public long SegmentId { get; set; }
            public required string Ref { get; set; }
            public int SortOrder { get; set; }
            public required string OriginalText { get; set; }
            public string? TypedText { get; set; }
            public string? ImageUrl { get; set; }
            public DateTime? CopiedAt { get; set; }
            public double? Accuracy { get; set; }
        }
    }
}
