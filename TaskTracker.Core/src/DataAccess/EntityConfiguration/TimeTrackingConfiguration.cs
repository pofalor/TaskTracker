using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Core.src.Entities;

namespace TaskTracker.Core.src.DataAccess.EntityConfiguration
{
    public class TimeTrackingConfiguration : BaseEntityConfiguration<TimeTracking>
    {
        public override void Configure(EntityTypeBuilder<TimeTracking> builder)
        {
            base.Configure(builder);
            builder.ToTable("TimeTracking");
            builder.Property(p => p.TimeSpent).HasColumnName("time_spent").HasDefaultValue(TimeSpan.Zero).IsRequired();
            builder.Property(p => p.DateBegin).HasColumnName("date_begin").IsRequired();
            builder.Property(p => p.Comment).HasColumnName("comment");
            builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).IsRequired();
            builder.HasOne(p => p.Issue).WithMany().HasForeignKey(p => p.IssueId).IsRequired();
            builder.Property(p => p.AutoTrackStatus).HasColumnName("auto_track_status").IsRequired(false);
        }
    }
}
