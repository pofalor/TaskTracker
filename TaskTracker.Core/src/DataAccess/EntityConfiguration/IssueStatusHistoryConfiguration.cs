using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Core.src.Entities;

namespace TaskTracker.Core.src.DataAccess.EntityConfiguration
{
    public class IssueStatusHistoryConfiguration : BaseEntityConfiguration<IssueStatusHistory>
    {
        public override void Configure(EntityTypeBuilder<IssueStatusHistory> builder)
        {
            base.Configure(builder);
            builder.ToTable("IssueStatusHistory");
            builder.Property(p => p.OldStatus).HasColumnName("old_status").IsRequired(false);
            builder.Property(p => p.NewStatus).HasColumnName("new_status").IsRequired();
            builder.Property(p => p.ChangedAt).HasColumnName("changed_at").IsRequired();
            builder.HasOne(p => p.Issue).WithMany().HasForeignKey(p => p.IssueId).IsRequired();
            builder.HasOne(p => p.ChangedByUser).WithMany().HasForeignKey(p => p.ChangedByUserId).IsRequired();
        }
    }
}
