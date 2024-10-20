using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TaskTracker.Core.src.DataAccess;

namespace TaskTracker.Web.Api.Ef
{
    public class IdentityDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationIdentityDbContext>
    {
        /// <inheritdoc />
        public ApplicationIdentityDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // получаем строку подключения из файла appsettings.json
            string connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Port=5434;Database=tasktracker;Username=postgres;Password=admin";

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationIdentityDbContext>();

            optionsBuilder.UseNpgsql(connectionString, opts => opts.CommandTimeout((int)TimeSpan.FromMinutes(10).TotalSeconds));
            return new ApplicationIdentityDbContext(optionsBuilder.Options);
        }
    }
}
