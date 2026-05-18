using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.NetworkInformation;
using TaskTracker.Core.src.BackgroundJobs;
using TaskTracker.Core.src.Repositories;
using TaskTracker.Core.src.Repositories.Impl;
using TaskTracker.Core.src.Services;
using TaskTracker.Core.src.Services.Impl;

namespace TaskTracker.Core.src.Installers
{
    public static class CoreInstaller
    {
        public static IServiceCollection AddCore(this IServiceCollection services) 
        { 
            services.AddCoreServices();
            services.AddBackgroundJobs();
            return services;
        }

        /// <summary>
        /// Установить сервисы
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ILogNotificatorService, LogNotificatorService>();
            services.AddScoped<ISosService, SosService>();
            services.AddScoped<IWorkspaceService, WorkspaceService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IIssueService, IssueService>();
            services.AddScoped<IAutoTimeTrackService, AutoTimeTrackService>();

            services.AddRepositories();

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<ITimeTrackingRepository, TimeTrackingRepository>();
            services.AddScoped<IIssueRepository, IssueRepository>();

            return services;
        }

        public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
        {
            services.AddHostedService<InviteBackgroundJob>();
            services.AddHostedService<RoleManagerBackgroundJob>();

            return services;
        }
    }
}
