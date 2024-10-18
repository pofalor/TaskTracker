using Microsoft.EntityFrameworkCore;
using TaskTracker.Core.src.DataAccess.EntityConfiguration;

namespace TaskTracker.Core.src.DataAccess
{
    /// <summary>
    /// DbContext приложения
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
        }
    }
}