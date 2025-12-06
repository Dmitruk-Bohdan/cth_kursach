using CTH.Api.Infrastructure;
using CTH.Database.Extensions;
using CTH.Database.Repositories.Interfaces;
using CTH.Services.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CTH.Common.Constants;
using System.IdentityModel.Tokens.Jwt;
using System;

namespace CTH.Api.Extensions;

public static class ApiServiceCollectionExtension
{
    public static IServiceCollection ConfigureApiLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddDatabaseInfrastructure(configuration)
            .ConfigureServicesLayer(configuration)
            .ConfigureLogging()
            .ConfigureCors()
            .ConfigureControllers()
            .ConfigureAuthentication();

        return services;
    }

    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Сentralized Testing Helper",
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Authorization header using the Bearer scheme.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new List<string>()
                }
            });
        });
        return services;
    }

    private static IServiceCollection ConfigureLogging(this IServiceCollection services)
    {
        services
            .AddLogging(logger => logger.AddLog4Net()
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning))
            .AddOptions();

        return services;
    }

    private static IServiceCollection ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("DevSiteCorsPolicy", devCorsBuilder =>
            {
                devCorsBuilder.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders(HeadersKeysConstants.PaginationKey, HeadersKeysConstants.ContentDisposition);

            });

            options.AddPolicy("ProdSiteCorsPolicy", devCorsBuilder =>
            {
                devCorsBuilder.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders(HeadersKeysConstants.PaginationKey, HeadersKeysConstants.ContentDisposition);

            });
        });

        return services;
    }

    private static IServiceCollection ConfigureControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ModelValidationAttribute>();
            options.Filters.Add<ExceptionHandlerAttribute>();
            options.Filters.Add<ResponseModelAttribute>();
            options.ReturnHttpNotAcceptable = true;
        });

        return services;
    }

    private static IServiceCollection ConfigureAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = AuthOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = AuthOptions.Audience,
                    ValidateLifetime = true,

                    IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                    ValidateIssuerSigningKey = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var jtiValue = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                        if (!Guid.TryParse(jtiValue, out var tokenId))
                        {
                            context.Fail("Token identifier is missing.");
                            return;
                        }

                        var sessionRepository = context.HttpContext.RequestServices.GetRequiredService<IUserSessionRepository>();
                        var isActive = await sessionRepository.IsSessionActiveAsync(tokenId, context.HttpContext.RequestAborted);

                        if (!isActive)
                        {
                            context.Fail("Token has been revoked or expired.");
                        }
                    }
                };
            });

        return services;
    }
}
