using System.Reflection;
using Buddham.Core.Entities.Ai;
using Buddham.Core.Entities.Chat;
using Buddham.Shared.Configurations;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using ChatMessage = Buddham.Core.Entities.Chat.ChatMessage;

namespace Buddham.Shared.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    // * Chat
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<RoomParticipant> RoomParticipants => Set<RoomParticipant>();

    // * Ai Chat
    public DbSet<AiChatMessage> AiChatMessages => Set<AiChatMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.RegisterModelCreating(SchemaTypes.Chat);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly(),
            type => typeof(IChatConfiguration).IsAssignableFrom(type) &&
                    type is { IsInterface: false, IsAbstract: false });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<Pgvector.Vector>().HaveColumnType(PgTypes.Vector);
        builder.Properties<DateTime>().HaveColumnType(PgTypes.TimestampTz);
        builder.Properties<DateTime?>().HaveColumnType(PgTypes.TimestampTz);
    }
}