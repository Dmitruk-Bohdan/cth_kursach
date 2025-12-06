using CTH.Services.Interfaces;
using CTH.Services.Mappings.Implementations;
using CTH.Services.Mappings.Interfaces;
using CTH.Services.Implementations;
using CTH.Services.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CTH.Services.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection ConfigureServicesLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .ConfigureSettings(configuration)
                .ConfigureServices()
                .ConfigureHttpClient()
                .ConfigureMappings();

            return services;
        }

        public static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            services
                .AddScoped<IUserAccoutManagmentService, UserAccoutManagmentService>()
                .AddScoped<IUserIdentityService, UserIdentityService>()
                .AddScoped<IUserSessionService, UserSessionService>()
                .AddScoped<IStudentTestService, StudentTestService>()
                .AddScoped<IStudentAttemptService, StudentAttemptService>()
                .AddScoped<IStudentStatisticsService, StudentStatisticsService>()
                .AddScoped<ITeacherTestService, TeacherTestService>();

            return services;
        }

        public static IServiceCollection ConfigureHttpClient(this IServiceCollection services)
        {
            //services.AddHttpClient<ILinkedInApiService, LinkedInApiService>();
            return services;
        }

        public static IServiceCollection ConfigureSettings(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));

            return services;
        }
        public static IServiceCollection ConfigureMappings(this IServiceCollection services)
        {
            services.AddScoped<IUserMapper, UserMapper>();

            return services;
        }


    }
}
