using Buddham.Core.Entities.Ai;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Pgvector;

namespace Buddham.Shared.Configurations.Ai;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>, IAiConfiguration
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages", SchemaTypes.Ai);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnType(PgTypes.BigInt);
        builder.Property(x => x.ChatSessionId).HasColumnType(PgTypes.BigInt).IsRequired();
        builder.Property(x => x.Role).IsRequired().HasMaxLength(20).HasColumnType(PgTypes.Text);
        builder.Property(x => x.Content).HasColumnType(PgTypes.Text);
        builder.Property(x => x.ThinkingContent).HasColumnType(PgTypes.Text);
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");

        builder.HasIndex(x => x.ChatSessionId).HasDatabaseName("ix_chat_messages_session_id");

        // 세션별 메시지 시간순 조회용 (커서 페이징 대응)
        builder.HasIndex(x => new { x.ChatSessionId, x.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_chat_messages_session_id_created_at");
        var tsVectorConverter = new ValueConverter<string?, NpgsqlTsVector?>(
            s => null,
            v => v == null ? null : v.ToString()
        );
        var vectorConverter = new ValueConverter<float[]?, Vector?>(
            v => v == null ? null : new Vector(v),
            v => v == null ? null : v.ToArray()
        );

        // tsvector - 대화내용 검색
        builder.Property(x => x.SearchVector)
            .HasColumnType("tsvector")
            .HasConversion(tsVectorConverter)
            .HasComputedColumnSql(
                "setweight(to_tsvector('simple', coalesce(content, '')), 'A')",
                stored: true);

        // vector - 의미론적 검색 (assistant 답변 임베딩)
        builder.Property(x => x.Embedding)
            .HasColumnType("vector(1024)")
            .HasConversion(vectorConverter);

        // 인덱스
        builder.HasIndex(x => x.SearchVector)
            .HasMethod("GIN")
            .HasDatabaseName("ix_chat_messages_search_vector");

        builder.HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasDatabaseName("ix_chat_messages_embedding")
            .HasOperators("vector_cosine_ops");
    }
}
