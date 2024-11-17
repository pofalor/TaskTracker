using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.DataAccess.EntityConfiguration
{
    public class WorkSpaceMemberConfiguration : BaseEntityConfiguration<WorkSpaceMember>
    {
        public override void Configure(EntityTypeBuilder<WorkSpaceMember> builder)
        {
            base.Configure(builder);
            builder.ToTable("WorkSpaceMember");
            builder.Property(p => p.TeamRole).HasColumnName("team_role").HasDefaultValue(UserTeamRole.NotSet);
            builder.Property(p => p.UserStatus).HasColumnName("user_status").HasDefaultValue(UserWorkSpaceStatus.Active);
            builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).IsRequired();
            builder.HasOne(p => p.WorkSpace).WithMany().HasForeignKey(p => p.WorkSpaceId).IsRequired();
        }
    }
}
