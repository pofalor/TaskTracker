using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.Constants;

namespace TaskTracker.Core.src.Installers
{
    /// <summary>
    /// Первичное наполнение Identity-хранилища системными данными
    /// </summary>
    public static class IdentityDataSeeder
    {
        /// <summary>
        /// Роли, без которых система не работает:
        /// <see cref="Permissions.UserRole"/> выдаётся каждому при регистрации,
        /// <see cref="Permissions.Admin"/> нужна для модерации корпоративных воркспейсов
        /// </summary>
        static readonly string[] SystemRoles = new[] { Permissions.UserRole, Permissions.Admin };

        /// <summary>
        /// Создать системные роли, если их ещё нет в БД.
        /// Вызывается на старте приложения после применения миграций, идемпотентно.
        /// </summary>
        /// <param name="serviceProvider">Провайдер сервисов приложения</param>
        /// <param name="cancellationToken">Токен отмены</param>
        public static async Task SeedIdentityRolesAsync(this IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(IdentityDataSeeder).FullName!);

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var roleName in SystemRoles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // RoleExistsAsync сравнивает по нормализованному имени, поэтому роль,
                // созданную ранее вручную через api/sos/createnewrole в другом регистре, повторно не создаём
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                var creationResult = await roleManager.CreateAsync(new IdentityRole(roleName));

                if (creationResult.Succeeded)
                {
                    logger.LogInformation("System role {RoleName} created.", roleName);
                    continue;
                }

                var errors = string.Join("; ", creationResult.Errors.Select(x => x.Description));
                var message = $"Cannot create system role {roleName}: {errors}";

                logger.LogError(message);
                throw new InvalidOperationException(message);
            }
        }
    }
}
