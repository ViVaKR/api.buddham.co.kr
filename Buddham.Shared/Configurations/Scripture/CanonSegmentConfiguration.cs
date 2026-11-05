using Buddham.Core.Entities.Scriptures;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions.Scripture;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buddham.Shared.Configurations.Scripture;

public class CanonSegmentConfiguration : ScriptureConfigurationBase<CanonSegment>
{
    protected override string TableName => "canon_segments";

    protected override void ConfigureEntity(EntityTypeBuilder<CanonSegment> builder)
    {
        builder.Property(x => x.SegmentRef).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Text).IsRequired().HasColumnType(PgTypes.Text);
        builder.Property(x => x.TextNormalized).HasColumnType(PgTypes.Text);
        builder.Property(x => x.PageRef).HasMaxLength(100);
        builder.Property(x => x.TextHash).HasMaxLength(64);
        builder.Property(x => x.EmbeddingModel).HasMaxLength(100);

        // 벡터 검색 (Guide와 동일한 방식)
        builder.ConfigureEmbedding(x => x.Embedding, "ix_canon_segments_embedding");

        // 부분 문자열 검색(한글·한문·팔리 공통): pg_trgm GIN
        builder.HasIndex(x => x.TextNormalized)
            .HasMethod(PgMethods.GIN)
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("ix_canon_segments_text_normalized_trgm");

        builder.HasActiveUniqueIndex(x => new { x.EditionId, x.SegmentRef }, "ux_canon_segments_edition_ref");
        builder.HasIndex(x => new { x.EditionId, x.SortOrder });

        builder.HasOne(x => x.Edition).WithMany(x => x.Segments).HasForeignKey(x => x.EditionId);
    }
}
