using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Core.src.Services;
using TaskTracker.Core.src.Services.Impl;

namespace TaskTracker.Core.src.Installers
{
    public static class CoreInstaller
    {
        public static IServiceCollection AddCore(this IServiceCollection services) 
        { 
            services.AddCoreServices();
            return services;
        }

        /// <summary>
        /// Установить сервисы
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<ILogNotificatorService, LogNotificatorService>();

            return services;
        }
    }
}
