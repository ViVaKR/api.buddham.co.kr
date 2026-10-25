using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Buddham.Core.Common.Options;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Entities.Comment;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Auth;
using Buddham.Core.Interfaces.Cache;
using Buddham.Core.Interfaces.Chat;
using Buddham.Core.Interfaces.Comment;
using Buddham.Core.Interfaces.Log;
using Buddham.Core.Interfaces.Ollama;
using Buddham.Core.Interfaces.Public;
using Buddham.Shared.Constants;
using Buddham.Shared.Data;
using Buddham.Shared.Filters;
using Buddham.Shared.Infrastructure;
using Buddham.Shared.Middlewares;
using Buddham.Shared.Protos;
using Buddham.Shared.Services.Auth;
using Buddham.Shared.Services.Cache;
using Buddham.Shared.Services.Chat;
using Buddham.Shared.Services.Comment;
using Buddham.Shared.Services.Log;
using Buddham.Shared.Services.OllamaService;
using Buddham.Shared.Services.Public;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Viva.Chat.Contracts;

namespace Buddham.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// (1) 인프라
    /// 기술적 설정 모음
    /// </summary>
    public static IServiceCollection AddInfrastructureConfigs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddHealthChecks();

        services.AddCors(options =>
        {
            options.AddPolicy(AppInfrastructure.CorsPolicy, policy =>
            {
                policy.SetIsOriginAllowed(origin => Origins.AllowedOriginsRegex.IsMatch(origin))
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromHours(2));
            });
        });

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = UploadSize.Video; // 1GB //  1_073_741_824;
            // form-data 의 테스트 필드 최대 길이
            // filename, description, JSON metadata
            // 텍스트 필드는 보통 몇 KB 수준
            // 너무 크면 suface 공격 증가
            options.ValueLengthLimit = 1024 * 1024;

            // 각 파트의 헤더 길이 제한
            // 기본값 보다 살짝 여유
            // 긴 filename, metadata 대응
            // Content-Disposition: form-data; name="file"; filename="aaaa..."
            options.MultipartHeadersLengthLimit = 32 * 1024;

            // (핵심) 초과하면 디스크로 스풀링 (임시 파일)
            // 대용량 업로드 성능/안정성에 직결
            // 여러 명 동시에 업로드 가능성 있음
            // 2MG ~4MB 가 안정적
            options.MemoryBufferThreshold = 2 * 1024 * 1024;
        });

        services.AddHttpClient();
        services.AddHttpContextAccessor();

        services.Configure<Core.Schemas.BuddhamAppSettings>(configuration.GetSection("WebUrls"));

        // 처리율 제한 (Tate Limiting)
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("email", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 3;
            });
        });

        return services;
    }

    /// <summary>
    /// 3개 프로젝트가 분산 환경(쿠버네티스)에서 인증 키를 공유하도록 다스리는 체 게바라 연합군 설정
    /// </summary>
    public static IServiceCollection AddCommonDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILoggingBuilder logging)
    {
        // 🛠️ 개발 환경이 아닐 때만 Redis 연합군 투입!
        if (environment.IsDevelopment()) return services;
        var redisConn = configuration.GetConnectionString(AppInfrastructure.RedisConnection)
                        ?? throw new InvalidOperationException("Redis connection string is missing for DataProtection!");

        // 🔥 핵심: 3개 프로젝트가 같은 암호화 키를 공유해야 하므로,
        // 공통 명칭(예: "Buddham.Shared.App")으로 애플리케이션 이름을 통일해 주는 게 가장 안전
        services.AddDataProtectionsWithRedis(AppInfrastructure.ProtectionName, redisConn);

        // * 🤫 자자한 경고 로그 차단으로 터미널 수호!
        logging.AddFilter("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager", LogLevel.Error);

        return services;
    }

    public static IServiceCollection AddSignalRService(this IServiceCollection services, IWebHostEnvironment env)
    {
        // 💬 [실시간 소통 특공대] 우주 최강 SignalR 엔진 빌더 설정
        services.AddSignalR(options =>
            {
                // 1. 🛠️ 에러 메시지 디테일 설정 (개발 환경에서만 친절하게)
                options.EnableDetailedErrors = env.IsDevelopment();

                // 2. ⚡ [핵심] 상태 유지 재연결(Stateful Reconnect) 엔진 가동!
                // 네트워크가 잠깐 끊겨도 유저당 최대 2,000개의 메시지까지 버퍼에 보관했다가 재연결 시 쏟아부어줌!
                options.StatefulReconnectBufferSize = 2_000;

                // 3. ⏳ 대규모 접속 및 모바일 환경을 위한 끊김 방지 타임아웃 튜닝
                // 클라이언트가 무응답으로 버틸 수 있는 한계 시간 (기본 30초 -> 60초)
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);

                // 서버가 "너 살아있냐?" 하고 생존 신호(Ping) 날리는 주기 (ClientTimeout의 최소 절반 이하 권장)
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);

                // 한 번에 주고받을 수 있는 최대 메시지 크기 (1MB로 넉넉하게 확장)
                options.MaximumReceiveMessageSize = 1024 * 1024;
            })
            // 🟡 A. JSON 프로토콜 최적화 (Angular 기본 및 대중적 통신용)
            .AddJsonProtocol(options =>
            {
                // 순환 참조(무한 루프) 에러 우주 밖으로 차단! (.NET Entity 연동 시 필수)
                options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                // 대소문자 구분 없이 유연하게 매핑하도록 설정
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
            })
            // 📦 B. MessagePack 프로토콜 최적화 (아까 챙긴 우주 초고속 압축 제트 엔진!)
            .AddMessagePackProtocol(options =>
            {
                // MessagePack 직렬화 시 표준 규격 및 순환 참조 방지 옵션 주입 가능 (필요 시 세부 튜닝)
                options.SerializerOptions = MessagePack.MessagePackSerializerOptions.Standard.WithSecurity(MessagePack.MessagePackSecurity.UntrustedData);
            });

        return services;
    }

    /// <summary>
    /// 2. 컨트롤러
    /// </summary>
    public static IServiceCollection AddControllerServices(this IServiceCollection services)
    {
        services.AddControllers(x =>
        {
            // 🌟 [정석] 임시로 컨테이너를 구워 로거를 공수하되, 스코프를 타서 누수 차단!
            using var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<EnforceHttpMethodConvention>>();
            // 실수 방지 전수조사 개시
            x.Conventions.Add(new EnforceHttpMethodConvention(logger, throwOnMissingMethod: false));
        }).AddJsonOptions(options => AppJsonOptions.Configure(options.JsonSerializerOptions));
        return services;
    }

    /// <summary>
    /// 4. Identity 보안설정
    /// </summary>
    public static IServiceCollection AddIndentityConfiguration(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(x =>
            {
                x.Password.RequireDigit = true;
                x.Password.RequiredLength = 8;
                x.Password.RequireNonAlphanumeric = true;
                x.Password.RequireUppercase = true;
                x.Password.RequireLowercase = true;

                x.User.RequireUniqueEmail = true;
                x.Lockout.AllowedForNewUsers = true;
                x.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                x.Lockout.MaxFailedAccessAttempts = 5;
                x.SignIn.RequireConfirmedEmail = false; // 이메일 확인없이 로그인 허용
            })
            .AddEntityFrameworkStores<BuddhamDbContext>()
            .AddDefaultTokenProviders()
            .AddRoles<ApplicationRole>()
            .AddSignInManager();


        // * 이메일/비밀번호 찾기 토큰 유효시간 설정
        services.Configure<DataProtectionTokenProviderOptions>(opt => opt.TokenLifespan = TimeSpan.FromHours(1));
        return services;
    }

    /// <summary>
    /// 5. AI & Vector 서비스
    /// </summary>
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Ollama 옵션 — 모든 프로젝트 공통
        services.AddOptions<OllamaChatOptions>()
            .Bind(configuration.GetSection(OllamaChatOptions.SectionName))
            .Validate(x => !string.IsNullOrEmpty(x.BaseUrl), "BaseUrl은 필수입니다.")
            .ValidateOnStart();

        var ollamaUrl = configuration["Ollama:BaseUrl"]
                        ?? throw new InvalidOperationException("Ollama:BaseUrl 설정이 빠졌습니다!");

        services.AddHttpClient<IOllamaService, OllamaService>(client =>
        {
            client.BaseAddress = new Uri(ollamaUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        var ollamaChatUrl = configuration["OllamaChat:BaseUrl"]
                            ?? throw new InvalidOperationException("OllamaChat:BaseUrl 설정이 빠졌습니다!");

        services.AddHttpClient<OllamaChatService>(client =>
        {
            client.BaseAddress = new Uri(ollamaChatUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // 👇 gRPC 클라이언트는 Api 프로젝트에서만!
        var grpcUrl = configuration["GrpcServices:AiService"];
        if (!string.IsNullOrEmpty(grpcUrl))
        {
            services.AddGrpcClient<AiEngine.AiEngineClient>(o => { o.Address = new Uri(grpcUrl); });
        }

        return services;
    }

    /// <summary>
    /// 6. Business Logic
    /// </summary>
    public static IServiceCollection AddBusinessService(
        this IServiceCollection services,
        IConfiguration configuration,
        BuddhamProjectMode mode = BuddhamProjectMode.Api) // 👈 기본값은 Api(전체 소환)
    {
        // 🛡️ 1. [공통 무기] 어떤 기지국이든 필수적으로 쓰는 기초 체력!
        services.AddScoped(typeof(ServiceDependencies<>));
        services.AddScoped<IFileValidationService, FileValidationService>();
        services.AddScoped<IFileManagerService, FileManagerService>();
        services.AddScoped(typeof(ValidateFilter<>));
        services.AddValidatorsFromAssemblyContaining<AuthService>();

        switch (mode)
        {
            // [Chat]
            case BuddhamProjectMode.Chat:
                services.AddScoped<IChatRoomService, ChatRoomService>();
                services.AddScoped<IChatMessageService, ChatMessageService>();
                services.AddScoped<IUserService, UserService>();
                services.AddScoped<IUserRoleService, UserRoleService>();
                services.AddScoped<ITokenService, TokenService>();
                services.AddScoped<IAuditLogService, AuditLogService>();
                services.AddSingleton<IChatStore, InMemoryChatStore>();
                return services;

            // [Ai]
            case BuddhamProjectMode.Ai:
                services.AddScoped<IUserService, UserService>();
                services.AddScoped<ITokenService, TokenService>();
                services.AddScoped<IAuditLogService, AuditLogService>();
                return services;

            // [Api]
            case BuddhamProjectMode.Api: break;
            default: throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        // 👑 4. [Api 기지국 풀 모드] 메인 서버용 무거운 중화기 군단 총출동!
        // ================== [이메일 연합군] ==================
        services.AddOptions<EmailConfigOptions>()
            .Bind(configuration.GetSection(EmailConfigOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.Configure<EmailConfigOptions>(configuration.GetSection(EmailConfigOptions.SectionName));

        services.AddScoped<IMailService, MailService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailAuthService, EmailAuthService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();

        // ================== [댓글 서비스] ==================
        services.AddScoped<ICommentService, CommentService>();

        // GuideComment
        services.AddScoped<GenericCommentService<GuideComment, GuideCommentLike>>(sp => new GenericCommentService<GuideComment, GuideCommentLike>(sp.GetRequiredService<PublicDbContext>()));

        // ================== [인증 및 코어 연합군] ==================
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // ================== [미디어/동영상 중화기 중대] ==================
        services.AddScoped<IImageProcessorService, ImageProcessorService>();
        services.AddScoped<IVideoProcessorService, VideoProcessorService>();
        services.AddScoped<IFfmpegRunner, FfmpegRunner>();
        services.AddSingleton<IVideoJobQueue, VideoJobQueue>();
        services.AddHostedService<VideoProcessingWorker>();

        // ====
        services.AddScoped<IAppleSyscallService, AppleSyscallService>();
        services.AddScoped<IGuideService, GuideService>();

        return services;
    }

    /// <summary>
    /// 7. PostgreSQL Database
    /// </summary>
    public static IServiceCollection AddDatabases(
        this IServiceCollection services,
        IConfiguration configuration,
        bool onlyChatContext = false,
        bool onlyAiContext = false)
    {
        var connectionString = configuration.GetConnectionString(AppInfrastructure.DefaultConnection) ??
        throw new Exception("Default connection string is missing!");

        if (onlyChatContext)
        {
            // 채팅방 굴릴 때 필수적인 '인증(Auth)'과 '채팅(Chat)' 영토만 쏙 골라 주입!
            RegisterBuddhamDbContext<BuddhamDbContext>(SchemaTypes.Auth);
            RegisterBuddhamDbContext<ChatDbContext>(SchemaTypes.Chat);
            RegisterBuddhamDbContext<AiDbContext>(SchemaTypes.Ai);
            RegisterBuddhamDbContext<LogDbContext>(SchemaTypes.Log);
            return services;
        }

        if (onlyAiContext)
        {
            RegisterBuddhamDbContext<BuddhamDbContext>(SchemaTypes.Auth);
            RegisterBuddhamDbContext<AiDbContext>(SchemaTypes.Ai);
            RegisterBuddhamDbContext<ChatDbContext>(SchemaTypes.Chat);
            RegisterBuddhamDbContext<LogDbContext>(SchemaTypes.Log);
            return services;
        }

        // 1. [Ai] 스키마 영토
        RegisterBuddhamDbContext<AiDbContext>(SchemaTypes.Ai);

        // 2. [Archive] 스키마 영토
        RegisterBuddhamDbContext<ArchiveDbContext>(SchemaTypes.Archive);

        // 3. [Auth] 스키마 영토 (🚨 복붙 탈출 완료!)
        RegisterBuddhamDbContext<BuddhamDbContext>(SchemaTypes.Auth);

        // 4. [Canon] 스키마 영토 (🚨 복붙 탈출 완료!)
        RegisterBuddhamDbContext<CanonDbContext>(SchemaTypes.Canon);

        // 5. [Chat] 스키마 영토 (🚨 복붙 탈출 완료!)
        RegisterBuddhamDbContext<ChatDbContext>(SchemaTypes.Chat);

        // 6. [Comment] 스키마 영토
        RegisterBuddhamDbContext<CommentDbContext>(SchemaTypes.Comment);

        // 7. [Dharma] 스키마 영토 (🚨 Comment 명찰 떼고 진짜 내 이름 찾음!)
        RegisterBuddhamDbContext<DharmaDbContext>(SchemaTypes.Dharma);

        // 8. [Log] 스키마 영토
        RegisterBuddhamDbContext<LogDbContext>(SchemaTypes.Log);

        // 9. [Public] 스키마 영토
        RegisterBuddhamDbContext<PublicDbContext>(SchemaTypes.Public);

        // 10. [Pitaka] 스키마 영토
        RegisterBuddhamDbContext<PitakaDbContext>(SchemaTypes.Pitaka);

        // 11. [Bhavana] 스키마 영토
        RegisterBuddhamDbContext<BhavanaDbContext>(SchemaTypes.Bhavana);

        // 12. [Desana] 스키마 영토
        RegisterBuddhamDbContext<DesanaDbContext>(SchemaTypes.Desana);

        // 13. [Upaya] 스키마 영토
        RegisterBuddhamDbContext<PujaDbContext>(SchemaTypes.Puja);

        // 14. [Upaya] 스키마 영토
        RegisterBuddhamDbContext<UpayaDbContext>(SchemaTypes.Upaya);


        return services;

        // 🛡️ 징기스칸 전용 멀티 스키마 자동 빌더 함수
        void RegisterBuddhamDbContext<TContext>(string schema) where TContext : DbContext
        {
            services.AddDbContext<TContext>(x =>
            {
                x.UseNpgsql(connectionString, npg =>
                {
                    npg.MigrationsAssembly(AppInfrastructure.MigrationsAssembly);
                    npg.UseNetTopologySuite(); // 공간 데이터 무기
                    npg.UseVector(); // 🤖 PgVector AI 벡터 무기

                    // 각 스키마 전용 마이그레이션 히스토리 테이블 분리 포진!
                    npg.MigrationsHistoryTable(AppInfrastructure.MigrationHistoryTable, schema);
                }).UseSnakeCaseNamingConvention(); // 스네이크 표기법 수호 (`buddham_user`)
            });
        }
    }

    /// <summary>
    /// 8. Redis
    /// </summary>
    public static IServiceCollection AddRedisDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // * Redis
        var redisConn = configuration.GetConnectionString(AppInfrastructure.RedisConnection) ??
                        throw new Exception("Redis connection string is missing!");

        // 1. 분산 캐시 시스템(IDistributedCache) 활성화
        services.AddStackExchangeRedisCache(options => options.Configuration = redisConn);
        // 2. 🌟 인터페이스와 구현체 모두를 커버하는 초강력 싱글톤 주입법!
        var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConn);
        services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);
        services.AddSingleton(connectionMultiplexer);
        services.AddSingleton<IRedisCacheService, RedisCacheService>();
        return services;
    }
}

/// <summary>
/// VivaKR.Chat (Nuget)
/// </summary>
public class InMemoryChatStore : IChatStore
{
    private readonly ConcurrentQueue<ChatMessage> _messages = new();

    public Task SaveAsync(ChatMessage message, CancellationToken ct = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChatMessage>> GetRecentAsync(string siteId, string room, int take, CancellationToken ct = default)
    {

        IReadOnlyList<ChatMessage> result = [.. _messages
            .Where(m => m.SiteId == siteId && m.Room == room)
            .OrderByDescending(m => m.SentAt)
            .Take(take)
            .OrderBy(m => m.SentAt)];

        return Task.FromResult(result);
    }
}
