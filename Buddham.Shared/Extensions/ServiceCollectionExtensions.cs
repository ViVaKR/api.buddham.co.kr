using Buddham.Core.Common.Options;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Entities.Comment;
using Buddham.Core.Interfaces.Auth;
using Buddham.Core.Interfaces.Cache;
using Buddham.Core.Interfaces.Comment;
using Buddham.Core.Interfaces.Common;
using Buddham.Core.Interfaces.Log;
using Buddham.Core.Interfaces.Ollama;
using Buddham.Core.Interfaces.Public;
using Buddham.Core.Interfaces.Tangwha;
using Buddham.Shared.Caching;
using Buddham.Shared.Constants;
using Buddham.Shared.Data;
using Buddham.Shared.Filters;
using Buddham.Shared.Infrastructure;
using Buddham.Shared.Middlewares;
using Buddham.Shared.Services.Auth;
using Buddham.Shared.Services.Cache;
using Buddham.Shared.Services.Comment;
using Buddham.Shared.Services.Common;
using Buddham.Shared.Services.Crud;
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
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

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
    /// 5. Vector 서비스
    /// </summary>
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
    {
        var ollamaUrl = configuration["Ollama:BaseUrl"]
                        ?? throw new InvalidOperationException("Ollama:BaseUrl 설정이 빠졌습니다!");

        services.AddHttpClient<IOllamaService, OllamaService>(client =>
        {
            client.BaseAddress = new Uri(ollamaUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        return services;
    }

    /// <summary>
    /// 6. Business Logic
    /// </summary>
    public static IServiceCollection AddBusinessService(this IServiceCollection services, IConfiguration configuration)
    {
        // 🛡️ 1. [공통 무기] 어떤 기지국이든 필수적으로 쓰는 기초 체력!
        services.AddScoped(typeof(ServiceDependencies<>));
        services.AddScoped<CrudDependencies>();
        services.AddScoped<IFileValidationService, FileValidationService>();
        services.AddScoped<IFileManagerService, FileManagerService>();
        services.AddScoped(typeof(ValidateFilter<>));
        services.AddValidatorsFromAssemblyContaining<AuthService>();

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

        // ================== [일반 비즈니스로직] ==================
        services.AddScoped<IGuideService, GuideService>();
        services.AddScoped<ITangwhaService, TangwhaService>();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEntityAuthorizationService, EntityAuthorizationService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();

        // ================== [댓글 서비스] =====================
        services.AddScoped<ICommentService, CommentService>();

        // ================== [개별댓글 서비스] ==================
        services.AddScoped(sp => new GenericCommentService<GuideComment, GuideCommentLike>(sp.GetRequiredService<PublicDbContext>()));
        services.AddScoped(x => new GenericCommentService<TangwhaComment, TangwhaCommentLike>(x.GetRequiredService<PublicDbContext>()));



        return services;
    }

    /// <summary>
    /// 7. PostgreSQL Database
    /// </summary>
    public static IServiceCollection AddDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(AppInfrastructure.DefaultConnection) ??
        throw new Exception("Default connection string is missing!");

        // 2. [Archive] 스키마 영토
        RegisterBuddhamDbContext<ArchiveDbContext>(SchemaTypes.Archive);

        // 3. [Auth] 스키마 영토 (🚨 복붙 탈출 완료!)
        RegisterBuddhamDbContext<BuddhamDbContext>(SchemaTypes.Auth);

        // 4. [Scripture] 스키마 영토
        RegisterBuddhamDbContext<ScriptureDbContext>(SchemaTypes.Scripture);

        // 6. [Comment] 스키마 영토
        RegisterBuddhamDbContext<CommentDbContext>(SchemaTypes.Comment);

        // 8. [Log] 스키마 영토
        RegisterBuddhamDbContext<LogDbContext>(SchemaTypes.Log);

        // 9. [Public] 스키마 영토
        RegisterBuddhamDbContext<PublicDbContext>(SchemaTypes.Public);

        return services;

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
    /// Redis 연결 1개를 모두가 공유한다.
    ///  - IConnectionMultiplexer : 컨테이너가 수명 관리(종료 시 Dispose)
    ///  - IDistributedCache      : 같은 연결을 재사용 (별도 연결 생성 방지)
    ///  - HybridCache            : L1(파드 메모리) + L2(Redis), 스탬피드 방지, 태그 무효화
    ///  - IRedisCacheService     : 기존 소비자(Auth/Scripture 등)를 위해 유지
    /// </summary>
    public static IServiceCollection AddRedisCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConn = configuration.GetConnectionString(AppInfrastructure.RedisConnection)
                        ?? throw new InvalidOperationException("Redis connection string is missing!");

        // 인스턴스 등록(AddSingleton(instance))은 컨테이너가 Dispose 하지 않는다 → 팩토리로 등록.
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

        // 키 접두사: 같은 Redis 를 다른 용도와 나눠 쓸 때 충돌 방지
        services.AddStackExchangeRedisCache(options => options.InstanceName = "Buddham:");

        // 위에서 만든 멀티플렉서를 IDistributedCache 가 재사용하도록 연결 (.NET 8+)
        services.AddOptions<RedisCacheOptions>()
            .Configure<IConnectionMultiplexer>((options, mux) =>
                options.ConnectionMultiplexerFactory = () => Task.FromResult(mux));

        // HybridCache 는 등록된 IDistributedCache(Redis)를 자동으로 L2 로 사용한다.
        services.AddHybridCache(options =>
        {
            // 기본 1MB 를 넘는 항목은 캐시되지 않는다. 목록 응답(본문 포함)을 고려해 상향.
            options.MaximumPayloadBytes = 4 * 1024 * 1024;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromSeconds(30)
            };
        });
        // 엔티티 단위 캐시 표준화(접두사/태그/TTL). HybridCache 위에서 동작한다.
        services.AddSingleton<IEntityCacheFactory, EntityCacheFactory>();

        services.AddSingleton<IRedisCacheService, RedisCacheService>();
        return services;
    }
}
