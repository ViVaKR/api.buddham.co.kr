using Buddham.Core.Entities.Chat;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Buddham.Shared.Configurations.Chat;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>, IChatConfiguration
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages", SchemaTypes.Chat);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName(ColumnNames.Id).HasColumnType(PgTypes.Uuid)
            .HasValueGenerator<SequentialGuidValueGenerator>();

        builder.Property(x => x.RoomId)
            .HasColumnName("room_id")
            .HasColumnType(PgTypes.Uuid)
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasColumnType(PgTypes.Uuid)
            .IsRequired();

        builder.Property(x => x.UserName)
            .HasMaxLength(50)
            .HasColumnName("user_name")
            .HasColumnType(PgTypes.Text);

        builder.Property(x => x.Avatar).HasColumnName("avatar").HasColumnType(PgTypes.Text);

        builder.Property(x => x.Content).HasColumnName("content").HasColumnType(PgTypes.Text);

        builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");

        // tsvector
        builder.Property(x => x.SearchVector)
            .HasColumnType(PgTypes.TsVector)
            .HasConversion(EfConverters.TsVectorConverter)
            .HasComputedColumnSql(
                "setweight(to_tsvector('simple', coalesce(\"content\", '')), 'A') || " +
                "setweight(to_tsvector('simple', coalesce(\"user_name\", '')), 'C')",
                stored: true);

        // vector
        builder.Property(x => x.Embedding)
            .HasColumnType("vector(1024)")
            .HasConversion(EfConverters.VectorConverter);

        // 💡 Full-Text Search 속도향상 GIN 인덱스
        builder.HasIndex(x => x.SearchVector).HasMethod("GIN");
        builder.HasIndex(x => new { x.SearchVector, x.CreatedAt }).HasMethod("GIN");

        // HNSW
        builder.HasIndex(x => x.Embedding)
            .HasMethod("GIN")
            .HasDatabaseName("ix_guides_search_vector");

        builder.HasIndex(x => x.RoomId);
        builder.HasIndex(x => x.UserId);

        builder.HasOne(m => m.Room)
            .WithMany(r => r.ChatMessages)
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
