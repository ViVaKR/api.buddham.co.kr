using Buddham.Core.Entities.Chat;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Buddham.Shared.Configurations.Chat;

public class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>, IChatConfiguration
{
    public void Configure(EntityTypeBuilder<ChatRoom> builder)
    {
        builder.ToTable("chat_rooms", SchemaTypes.Chat);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType(PgTypes.Uuid)
            .HasValueGenerator<SequentialGuidValueGenerator>();
    }
}
