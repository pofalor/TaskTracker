using Microsoft.EntityFrameworkCore;
using TaskTracker.Core.src.Entities;

namespace TaskTracker.DataAccess.src
{
    /// <summary>
    /// DbContext приложения
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=tasktracker;Username=postgres;Password=admin");
        }
    }
}