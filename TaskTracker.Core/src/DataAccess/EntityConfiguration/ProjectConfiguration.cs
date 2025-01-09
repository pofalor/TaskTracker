using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Core.src.Entities;

namespace TaskTracker.Core.src.DataAccess.EntityConfiguration
{
    public class ProjectConfiguration : BaseEntityConfiguration<Project>
    {
        public override void Configure(EntityTypeBuilder<Project> builder)
        {
            base.Configure(builder);
            builder.ToTable("Project");
            builder.Property(p => p.Name).HasColumnName("name").IsRequired();
            builder.Property(p => p.Description).HasColumnName("description").IsRequired(false);
            builder.Property(p => p.Code).HasColumnName("code").IsRequired();
            builder.Property(p => p.StartDate).HasColumnName("start_date");
            builder.Property(p => p.EndDate).HasColumnName("end_date").IsRequired(false);
            builder.HasOne(p => p.Author).WithMany().HasForeignKey(p => p.AuthorId).IsRequired();
            builder.HasOne(p => p.ProjectMgr).WithMany().HasForeignKey(p => p.ProjectMgrId).IsRequired();
            builder.HasOne(p => p.WorkSpace).WithMany().HasForeignKey(p => p.WorkSpaceId).IsRequired();
        }
    }
}
