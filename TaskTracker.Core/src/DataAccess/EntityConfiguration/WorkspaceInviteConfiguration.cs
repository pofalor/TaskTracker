using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.DataAccess.EntityConfiguration
{
    public class WorkspaceInviteConfiguration : BaseEntityConfiguration<WorkspaceInvite>
    {
        public override void Configure(EntityTypeBuilder<WorkspaceInvite> builder)
        {
            base.Configure(builder);
            builder.ToTable("WorkspaceInvite");
            builder.Property(p => p.Date).HasColumnName("Date");
            builder.Property(p => p.PreviousStatus).HasColumnName("previous_status");
            builder.Property(p => p.NewStatus).HasColumnName("new_status").IsRequired();
            builder.Property(p => p.RequestStatus).HasColumnName("request_status").HasDefaultValue(InviteStatus.Default);
            builder.Property(p => p.IsChecked).HasColumnName("is_checked").HasDefaultValue(false);
            builder.HasOne(p => p.WorkSpace).WithMany().HasForeignKey(p => p.WorkSpaceId).IsRequired();
            builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).IsRequired();
            builder.HasOne(p => p.Inviter).WithMany().HasForeignKey(p => p.InviterId).IsRequired();
            builder.Property(p => p.IsHidden).HasColumnName("is_hidden").HasDefaultValue(false);
        }
    }
}
