using Buddham.Core.Entities.Chat;
using Buddham.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Buddham.Shared.Configurations.Chat;

public class RoomPaticipantConfiguration : IEntityTypeConfiguration<RoomParticipant>, IChatConfiguration
{
    public void Configure(EntityTypeBuilder<RoomParticipant> builder)
    {
        builder.ToTable("room_participants", SchemaTypes.Chat);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType(PgTypes.Uuid)
            .HasValueGenerator<SequentialGuidValueGenerator>();
    }
}
