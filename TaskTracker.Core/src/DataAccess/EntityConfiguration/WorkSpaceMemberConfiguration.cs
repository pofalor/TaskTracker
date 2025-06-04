using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.DataAccess.EntityConfiguration
{
    public class WorkspaceMemberConfiguration : BaseEntityConfiguration<WorkspaceMember>
    {
        public override void Configure(EntityTypeBuilder<WorkspaceMember> builder)
        {
            base.Configure(builder);
            builder.ToTable("WorkspaceMember");
            builder.Property(p => p.TeamRole).HasColumnName("team_role").HasDefaultValue(UserTeamRole.NotSet);
            builder.Property(p => p.UserStatus).HasColumnName("user_status").HasDefaultValue(UserWorkspaceStatus.Active);
            builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).IsRequired();
            builder.HasOne(p => p.Workspace).WithMany().HasForeignKey(p => p.WorkspaceId).IsRequired();
        }
    }
}
