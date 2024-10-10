using Microsoft.EntityFrameworkCore;
using TaskTracker.Core.src.Entities;

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

        public DbSet<User> Users { get; set; } = null!;

    }
}