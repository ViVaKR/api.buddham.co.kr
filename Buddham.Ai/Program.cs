using Buddham.Ai.Services;
using Buddham.Core.EnumTypes;
using Buddham.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 💡 50051 포트를 HTTP/2 전용 gRPC 고속도로 요금소로 지정!
// builder.WebHost.ConfigureKestrel(options => { options.ListenAnyIP(50051, listenOptions => { listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2; }); });

builder.Services.AddCommonDataProtection(builder.Configuration, builder.Environment, builder.Logging);

// * 1 인프라
builder.Services.AddInfrastructureConfigs(builder.Configuration);

// * 2 컨트롤러
builder.Services.AddControllerServices();

// * 3 JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddGrpc();

// * 5. Ai & Vector
builder.Services.AddAiServices(builder.Configuration);

// * 6 Business Logic
builder.Services.AddBusinessService(builder.Configuration, BuddhamProjectMode.Ai);

// * 7 PostgreSQL Database
builder.Services.AddDatabases(builder.Configuration, onlyAiContext: true);

// * 8 Redis
builder.Services.AddRedisDatabase(builder.Configuration);

var app = builder.Build();

app.UseBuddhamMiddlewares();

app.MapGrpcService<AiStreamService>();

app.MapGet("/", () => "🤖 Buddham.Ai gRPC Server is running running running!!!");

app.MapControllers();

app.UseCommonHealthChecks();

app.Run();
