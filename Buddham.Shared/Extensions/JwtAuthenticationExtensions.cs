using Buddham.Core.Common.Options;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Buddham.Shared.Extensions;

public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// JWT Authentication
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // services.AddOptions<JwtOptions>()
        //     .Bind(configuration.GetSection(JwtOptions.SectionName))
        //     .ValidateDataAnnotations()
        //     .ValidateOnStart();
        //
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => Encoding.UTF8.GetByteCount(o.Key) >= 64,
                "JwtConfig:Key 는 UTF-8 기준 64바이트 이상이어야 합니다 (HS512).")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience),
                "JwtConfig:Issuer, Audience 가 필요합니다.")
            .Validate(o => o.AccessTokenIn > 0 && o.RefreshTokenIn > 0,
                "JwtConfig:AccessTokenIn, RefreshTokenIn 은 0보다 커야 합니다.")
            .ValidateOnStart();

        var jwtOptions = new JwtOptions();
        var section = configuration.GetSection(JwtOptions.SectionName);
        section.Bind(jwtOptions);

        // DI 컨테이너에도 나중에 쓰라고 등록!
        services.Configure<JwtOptions>(section);

        // 2. 🔑 보안 키 복호화 (정확하게 옵션 클래스의 Key 명칭을 저격!)
        var key = Encoding.UTF8.GetBytes(jwtOptions.Key);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.SaveToken = true;
                x.RequireHttpsMetadata = false;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha512],
                    NameClaimType = JwtRegisteredClaimNames.Sub
                };
                x.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var path = context.HttpContext.Request.Path;
                        if (path.StartsWithSegments("/ChatHub"))
                        {
                            var accessToken = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(accessToken))
                                context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}
