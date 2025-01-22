using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.DataAccess.EntityConfiguration
{
    public class IssueConfiguration : BaseEntityConfiguration<Issue>
    {
        public override void Configure(EntityTypeBuilder<Issue> builder)
        {
            base.Configure(builder);
            builder.ToTable("Issue");
            builder.Property(p => p.Name).HasColumnName("name").IsRequired();
            builder.Property(p => p.Description).HasColumnName("description");
            builder.Property(p => p.Type).HasColumnName("type").HasDefaultValue(IssueType.Task);
            builder.Property(p => p.Status).HasColumnName("status").HasDefaultValue(IssueStatus.Backlog);
            builder.Property(p => p.Priority).HasColumnName("priority").HasDefaultValue(IssuePriority.Medium);
            builder.Property(p => p.Estimate).HasColumnName("estimate").HasDefaultValue(TimeSpan.Zero);
            builder.HasIndex(b => b.Index).HasDatabaseName("index");
            builder.HasOne(p => p.Epic).WithMany().HasForeignKey(p => p.EpicId).IsRequired(false);
            builder.HasOne(p => p.Author).WithMany().HasForeignKey(p => p.AuthorId).IsRequired();
            builder.HasOne(p => p.Assignee).WithMany().HasForeignKey(p => p.AssigneeId).IsRequired(false);
            builder.HasOne(p => p.Project).WithMany().HasForeignKey(p => p.ProjectId).IsRequired();
            builder.HasIndex(p => new { p.ProjectId, p.Index }).IsUnique(true);
        }
    }
}
