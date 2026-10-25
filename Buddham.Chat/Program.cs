using Buddham.Chat.Hubs;
using Buddham.Core.EnumTypes;
using Buddham.Shared.Extensions;
using Buddham.Shared.Protos;
using Microsoft.AspNetCore.Http.Connections;
using Viva.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommonDataProtection(builder.Configuration, builder.Environment, builder.Logging);

// * 1 인프라
builder.Services.AddInfrastructureConfigs(builder.Configuration);

// * 2 컨트롤러
builder.Services.AddControllerServices();

// * 3 JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// SignalR
builder.Services.AddSignalRService(builder.Environment);

// * 5. Ai & Vector
builder.Services.AddAiServices(builder.Configuration);

// * 6 Business Logic
builder.Services.AddBusinessService(builder.Configuration, BuddhamProjectMode.Chat);

// * 7 PostgreSQL Database
builder.Services.AddDatabases(builder.Configuration, onlyChatContext: true);

// * 8 Redis
builder.Services.AddRedisDatabase(builder.Configuration);

builder.Services.AddGrpcClient<AiEngine.AiEngineClient>(o =>
{
    o.Address = new Uri(builder.Configuration["GrpcServices:AiService"] ?? "http://buddham-ai-svc:50051");
});

builder.Services.AddVivaChat(x =>
{
    x.DefaultSiteId = "buddham";
    x.AllowedSites = ["buddham", "test", "demo"];
});

var app = builder.Build();

app.UseBuddhamMiddlewares();

app.MapControllers();

app.UseCommonHealthChecks();

app.MapGet("/", () => Results.Ok(new
{
    service = "Buddham.Chat",
    version = "v1",
    status = "OK",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapVivaChat();

// app.MapHub<ChatHub>("/ChatHub", options =>
// {
//     // 1. 전송 방식 제한
//     options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents;
//     options.AllowStatefulReconnects = true;
//     options.ApplicationMaxBufferSize = 131072; // 128kb // 65536;
//     options.TransportMaxBufferSize = 131072;
// }).RequireAuthorization();

app.MapHub<AiChatHub>("/AiChatHub", options =>
{
    options.Transports = HttpTransportType.WebSockets;
    options.ApplicationMaxBufferSize = 262144;
    options.TransportMaxBufferSize = 262144;
});

app.Run();
