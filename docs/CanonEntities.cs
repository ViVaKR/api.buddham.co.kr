// =====================================================================
// 경장(Sutta-piṭaka) canon 스키마 → EF Core Code-First 엔터티
// 대상: .NET 10 / EF Core 10 / Npgsql.EntityFrameworkCore.PostgreSQL
// 필요 패키지:
//   Npgsql.EntityFrameworkCore.PostgreSQL
//   Pgvector.EntityFrameworkCore        (vector(1024) → Pgvector.Vector)
// 참고: Configuration(Fluent API, 복합키, 인덱스, 생성컬럼, enum 매핑 등)은 포함하지 않음
// =====================================================================
#nullable enable

using System.Text.Json;
using NpgsqlTypes;
using Pgvector;

namespace Canon.Entities;

// ---------------------------------------------------------------------
// 0. ENUM
// ---------------------------------------------------------------------
public enum Tradition
{
    Pali,
    ChineseAgama,
    Sanskrit,
    Tibetan,
    Gandhari,
    Other
}

public enum NodeKind
{
    Collection,   // 부(部)
    Work,         // 한 권의 책
    Division,     // 품/장/상윳따/닙빠따
    Text,         // 개별 경 또는 독립 게송군
    Section       // 경 내부의 큰 마디
}

public enum SegmentKind
{
    Prose,
    Verse,
    Refrain,
    Uddana,
    Title,
    Colophon,
    NarrativeFrame,
    Formula
}

public enum ReviewStatus
{
    Draft,
    AiReviewed,
    ScholarReviewed,
    Approved,
    Deprecated
}

// ---------------------------------------------------------------------
// 1. 전승 / 판본
// ---------------------------------------------------------------------
public class SourceEdition
{
    public short Id { get; set; }
    public string Code { get; set; } = null!;          // 'pts','bjt','cst4','sc-ms','cbeta-t'
    public Tradition Tradition { get; set; }
    public string Name { get; set; } = null!;
    public string Language { get; set; } = null!;      // BCP47
    public string Script { get; set; } = null!;        // 'Latn','Deva','Sinh','Hant'
    public string? License { get; set; }
    public string? Url { get; set; }
    public bool IsBase { get; set; }

    public ICollection<SegmentText> SegmentTexts { get; set; } = [];
    public ICollection<TranslationRun> TranslationRuns { get; set; } = [];
}

// ---------------------------------------------------------------------
// 2. 경장 구조 트리
// ---------------------------------------------------------------------
public class Node
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Path { get; set; } = null!;          // ltree: 'kn.snp.v1.s1'
    public NodeKind Kind { get; set; }
    public Tradition Tradition { get; set; } = Tradition.Pali;
    public string Uid { get; set; } = null!;           // 'snp1.1'
    public int Ordinal { get; set; }
    public string? NumberLabel { get; set; }
    public string TitleOrig { get; set; } = null!;
    public string TitleOrigNorm { get; private set; } = null!;   // 생성 컬럼 (읽기 전용)
    public JsonDocument Meta { get; set; } = JsonDocument.Parse("{}");
    public DateTimeOffset CreatedAt { get; set; }

    public Node? Parent { get; set; }
    public ICollection<Node> Children { get; set; } = [];
    public ICollection<NodeTitle> Titles { get; set; } = [];
    public ICollection<Segment> Segments { get; set; } = [];

    // 병행경 (node_a < node_b 규칙)
    public ICollection<NodeParallel> ParallelsAsA { get; set; } = [];
    public ICollection<NodeParallel> ParallelsAsB { get; set; } = [];

    public ICollection<CompositionItem> CompositionItems { get; set; } = [];
}

public class NodeTitle
{
    public Guid NodeId { get; set; }
    public string Lang { get; set; } = null!;          // 'ko','en','zh-Hant'
    public string Kind { get; set; } = "title";        // title / short / alias / traditional_name
    public string Value { get; set; } = null!;
    public ReviewStatus Status { get; set; } = ReviewStatus.Draft;

    public Node Node { get; set; } = null!;
}

public class NodeParallel
{
    public Guid NodeAId { get; set; }
    public Guid NodeBId { get; set; }
    public string Relation { get; set; } = null!;      // full / partial / verse_only / reference
    public string? Note { get; set; }

    public Node NodeA { get; set; } = null!;
    public Node NodeB { get; set; } = null!;
}

// ---------------------------------------------------------------------
// 3. 세그먼트
// ---------------------------------------------------------------------
public class Segment
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    public string Ref { get; set; } = null!;           // 'snp1.1:1.1' (절대 변경 금지)
    public int Ordinal { get; set; }
    public SegmentKind Kind { get; set; }
    public string? VerseNo { get; set; }
    public Guid? SpeakerId { get; set; }
    public JsonDocument Meta { get; set; } = JsonDocument.Parse("{}");

    public Node Node { get; set; } = null!;
    public Entity? Speaker { get; set; }

    public ICollection<SegmentText> Texts { get; set; } = [];
    public ICollection<SegmentTranslation> Translations { get; set; } = [];
    public ICollection<SegmentTerm> Terms { get; set; } = [];
    public ICollection<SegmentEntity> Entities { get; set; } = [];
    public ICollection<SegmentConcept> Concepts { get; set; } = [];
    public ICollection<SegmentEmbedding> Embeddings { get; set; } = [];
    public ICollection<Annotation> Annotations { get; set; } = [];
    public ICollection<Annotation> SpanEndAnnotations { get; set; } = [];
    public ICollection<CompositionItem> CompositionItems { get; set; } = [];
}

public class SegmentText
{
    public Guid SegmentId { get; set; }
    public short EditionId { get; set; }
    public string Text { get; set; } = null!;
    public string TextNorm { get; private set; } = null!;        // 생성 컬럼
    public NpgsqlTsVector Tsv { get; private set; } = null!;     // 생성 컬럼
    public string? VariantNote { get; set; }

    public Segment Segment { get; set; } = null!;
    public SourceEdition Edition { get; set; } = null!;
}

// ---------------------------------------------------------------------
// 4. 번역 층
// ---------------------------------------------------------------------
public class Translator
{
    public short Id { get; set; }
    public string Kind { get; set; } = null!;          // 'ai' | 'human'
    public string Name { get; set; } = null!;
    public string? Version { get; set; }

    public ICollection<TranslationRun> Runs { get; set; } = [];
}

public class TranslationRun
{
    public Guid Id { get; set; }
    public short TranslatorId { get; set; }
    public short BaseEditionId { get; set; }
    public string TargetLang { get; set; } = null!;
    public string? StyleGuide { get; set; }
    public string? PromptHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Translator Translator { get; set; } = null!;
    public SourceEdition BaseEdition { get; set; } = null!;
    public ICollection<SegmentTranslation> Translations { get; set; } = [];
}

public class SegmentTranslation
{
    public Guid Id { get; set; }
    public Guid SegmentId { get; set; }
    public Guid RunId { get; set; }
    public string Lang { get; set; } = null!;
    public string Text { get; set; } = null!;
    public string? LiteralGloss { get; set; }
    public string? TranslatorNote { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Draft;
    public bool IsCurrent { get; set; }
    public int Version { get; set; } = 1;
    public string? ReviewedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public NpgsqlTsVector Tsv { get; private set; } = null!;     // 생성 컬럼

    public Segment Segment { get; set; } = null!;
    public TranslationRun Run { get; set; } = null!;
    public ICollection<SegmentEmbedding> Embeddings { get; set; } = [];
}

// ---------------------------------------------------------------------
// 5. 용어집
// ---------------------------------------------------------------------
public class Term
{
    public Guid Id { get; set; }
    public string Lemma { get; set; } = null!;
    public string Language { get; set; } = "pi";
    public string? Pos { get; set; }
    public string? Etymology { get; set; }
    public string? DefinitionOrig { get; set; }

    public ICollection<TermRendering> Renderings { get; set; } = [];
    public ICollection<SegmentTerm> Occurrences { get; set; } = [];
}

public class TermRendering
{
    public Guid TermId { get; set; }
    public string Lang { get; set; } = null!;
    public string Rendering { get; set; } = null!;
    public string Policy { get; set; } = null!;        // fixed / contextual / transliterate
    public string? ContextNote { get; set; }

    public Term Term { get; set; } = null!;
}

public class SegmentTerm
{
    public Guid SegmentId { get; set; }
    public Guid TermId { get; set; }
    public string Surface { get; set; } = null!;
    public int? CharStart { get; set; }
    public int? CharEnd { get; set; }

    public Segment Segment { get; set; } = null!;
    public Term Term { get; set; } = null!;
}

// ---------------------------------------------------------------------
// 6. 개체 / 교리 분류 / 주석
// ---------------------------------------------------------------------
public class Entity
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = null!;          // person / place / deity / group / animal / object
    public string NameOrig { get; set; } = null!;
    public JsonDocument Attrs { get; set; } = JsonDocument.Parse("{}");

    public ICollection<Segment> SpokenSegments { get; set; } = [];
    public ICollection<SegmentEntity> SegmentLinks { get; set; } = [];
}

public class SegmentEntity
{
    public Guid SegmentId { get; set; }
    public Guid EntityId { get; set; }
    public string Role { get; set; } = null!;          // speaker / addressee / mentioned / setting

    public Segment Segment { get; set; } = null!;
    public Entity Entity { get; set; } = null!;
}

public class Concept
{
    public Guid Id { get; set; }
    public string Path { get; set; } = null!;          // ltree: 'teaching.four_noble_truths.dukkha'
    public string LabelOrig { get; set; } = null!;
    public string? LabelKo { get; set; }
    public string? Description { get; set; }

    public ICollection<SegmentConcept> SegmentLinks { get; set; } = [];
}

public class SegmentConcept
{
    public Guid SegmentId { get; set; }
    public Guid ConceptId { get; set; }
    public float Relevance { get; set; } = 1.0f;       // 0..1
    public string AssignedBy { get; set; } = "ai";
    public ReviewStatus Status { get; set; } = ReviewStatus.Draft;

    public Segment Segment { get; set; } = null!;
    public Concept Concept { get; set; } = null!;
}

public class Annotation
{
    public Guid Id { get; set; }
    public Guid SegmentId { get; set; }
    public Guid? SpanEndSegmentId { get; set; }
    public string Type { get; set; } = null!;          // simile / list / commentary / cross_ref / variant
    public JsonDocument Body { get; set; } = null!;
    public ReviewStatus Status { get; set; } = ReviewStatus.Draft;

    public Segment Segment { get; set; } = null!;
    public Segment? SpanEndSegment { get; set; }
}

// ---------------------------------------------------------------------
// 7. 조립 층
// ---------------------------------------------------------------------
public class Composition
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Kind { get; set; } = null!;          // anthology / theme / chanting / study_path / daily_verse
    public string? Owner { get; set; }
    public bool IsPublic { get; set; }

    public ICollection<CompositionItem> Items { get; set; } = [];
}

public class CompositionItem
{
    public Guid CompositionId { get; set; }
    public int Ordinal { get; set; }
    public Guid? SegmentId { get; set; }               // segment / node 중 정확히 하나
    public Guid? NodeId { get; set; }
    public string? Caption { get; set; }

    public Composition Composition { get; set; } = null!;
    public Segment? Segment { get; set; }
    public Node? Node { get; set; }
}

// ---------------------------------------------------------------------
// 8. 임베딩
//  ※ 원본 SQL은 PK에 nullable 컬럼(translation_id)이 포함되어 PostgreSQL에서 생성 자체가
//    실패합니다(PK 컬럼은 자동 NOT NULL). 그래서 대리키 Id를 두었습니다.
//    (segment_id, source, model, translation_id) 유일성은 Configuration 단계에서
//    UNIQUE 인덱스(NULLS NOT DISTINCT, PG15+)로 처리하는 것을 권합니다.
// ---------------------------------------------------------------------
public class SegmentEmbedding
{
    public Guid Id { get; set; }
    public Guid SegmentId { get; set; }
    public string Source { get; set; } = null!;        // 'orig' | 'translation'
    public Guid? TranslationId { get; set; }
    public string Model { get; set; } = null!;
    public Vector Embedding { get; set; } = null!;     // vector(1024)

    public Segment Segment { get; set; } = null!;
    public SegmentTranslation? Translation { get; set; }
}

// ---------------------------------------------------------------------
// 9. 뷰: v_current_reading (키 없는 엔터티 → HasNoKey().ToView(...))
// ---------------------------------------------------------------------
public class CurrentReadingView
{
    public string Ref { get; set; } = null!;
    public string TextUid { get; set; } = null!;
    public string Path { get; set; } = null!;
    public int Ordinal { get; set; }
    public SegmentKind Kind { get; set; }
    public string? Original { get; set; }
    public string? Korean { get; set; }
}
