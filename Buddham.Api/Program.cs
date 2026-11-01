using Buddham.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// * Protection
builder.Services.AddCommonDataProtection(builder.Configuration, builder.Environment, builder.Logging);

// * 1 인프라
builder.Services.AddInfrastructureConfigs(builder.Configuration);

// * 2 컨트롤러
builder.Services.AddControllerServices();

// * 4. Identity
builder.Services.AddIndentityConfiguration();

// * 3 JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// * 5. Ai & Vector
builder.Services.AddAiServices(builder.Configuration);

// * 6 Business Logic
builder.Services.AddBusinessService(builder.Configuration);

// * 7 PostgreSQL Database
builder.Services.AddDatabases(builder.Configuration);

// * 8 Redis
builder.Services.AddRedisDatabase(builder.Configuration);

var app = builder.Build();

app.UseBuddhamMiddlewares();

app.MapControllers();

app.UseCommonHealthChecks();

await app.RunAsync();
