using Microsoft.EntityFrameworkCore;
using TaskTracker.Core.src.Entities;
using TaskTracker.DataAccess.src.EntityConfiguration;

namespace TaskTracker.DataAccess.src
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

        //public DbSet<User> Users { get; set; } = null!;

    }
}