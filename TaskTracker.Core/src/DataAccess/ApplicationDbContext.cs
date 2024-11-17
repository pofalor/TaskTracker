using Microsoft.EntityFrameworkCore;
using TaskTracker.Core.src.DataAccess.BaseClasses;
using TaskTracker.Core.src.DataAccess.EntityConfiguration;
using TaskTracker.Utils.src.Extensions;

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
            modelBuilder.ApplyConfiguration(new WorkSpaceConfiguration());
            modelBuilder.ApplyConfiguration(new WorkSpaceMemberConfiguration());
            modelBuilder.ApplyConfiguration(new ProjectConfiguration());
            modelBuilder.ApplyConfiguration(new IssueConfiguration());
            modelBuilder.ApplyConfiguration(new TimeTrackingConfiguration());
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            UpdatePersistentEntitiesData();

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <inheritdoc />
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            UpdatePersistentEntitiesData();

            return base.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public override int SaveChanges()
        {
            UpdatePersistentEntitiesData();

            return base.SaveChanges();
        }

        private void UpdatePersistentEntitiesData()
        {
            var addedEntries = ChangeTracker
                .Entries<PersistentEntity>()
                .Where(x => x.State == EntityState.Added);

            var now = DateTime.UtcNow;

            addedEntries.Foreach(entry =>
            {
                entry.Entity.ObjectCreateDate = now;
                entry.Entity.ObjectEditDate = now;
            });

            var updateEntries = ChangeTracker
                .Entries<PersistentEntity>()
                .Where(x => x.State == EntityState.Modified);

            updateEntries.Foreach(entry =>
            {
                entry.Entity.ObjectEditDate = now;
            });
        }
    }
}