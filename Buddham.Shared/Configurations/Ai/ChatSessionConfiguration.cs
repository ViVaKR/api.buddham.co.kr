using Buddham.Core.Entities.Ai;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NpgsqlTypes;
using Pgvector;

namespace Buddham.Shared.Configurations.Ai;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>, IAiConfiguration
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("chat_sessions", SchemaTypes.Ai);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200).HasColumnType(PgTypes.Text);
        builder.Property(x => x.IsPublic).HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);

        builder.HasMany(x => x.Messages)
            .WithOne(x => x.ChatSession)
            .HasForeignKey(x => x.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_chat_sessions_user_id");
        builder.HasIndex(x => x.IsPublic).HasDatabaseName("ix_chat_sessions_is_public")
            .HasFilter("is_public = true");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_chat_sessions_created_at");
        builder.HasIndex(x => x.DeletedAt).HasDatabaseName("ix_chat_sessions_deleted_at")
            .HasFilter("deleted_at IS NULL");

        // AuditLog 패턴 그대로!
        var tsVectorConverter = new ValueConverter<string?, NpgsqlTsVector?>(
            s => null,
            v => v == null ? null : v.ToString()
        );
        var vectorConverter = new ValueConverter<float[]?, Vector?>(
            v => v == null ? null : new Vector(v),
            v => v == null ? null : v.ToArray()
        );

        // tsvector - 제목 검색
        builder.Property(x => x.SearchVector)
            .HasColumnType("tsvector")
            .HasConversion(tsVectorConverter)
            .HasComputedColumnSql(
                "setweight(to_tsvector('simple', coalesce(title, '')), 'A')",
                stored: true);

        // vector - 의미론적 검색
        builder.Property(x => x.Embedding)
            .HasColumnType("vector(1024)")
            .HasConversion(vectorConverter);

        // 인덱스
        builder.HasIndex(x => x.SearchVector)
            .HasMethod("GIN")
            .HasDatabaseName("ix_chat_sessions_search_vector");

        builder.HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasDatabaseName("ix_chat_sessions_embedding")
            .HasOperators("vector_cosine_ops");
    }
}