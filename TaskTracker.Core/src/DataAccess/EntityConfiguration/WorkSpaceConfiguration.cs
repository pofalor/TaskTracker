using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Core.src.Entities;

namespace TaskTracker.Core.src.DataAccess.EntityConfiguration
{
    public class WorkSpaceConfiguration : BaseEntityConfiguration<WorkSpace>
    {
        public override void Configure(EntityTypeBuilder<WorkSpace> builder)
        {
            base.Configure(builder);
            builder.ToTable("WorkSpace");
            builder.Property(p => p.Name).HasColumnName("name").IsRequired();
            builder.Property(p => p.WorkSpaceType).HasColumnName("work_space_type").HasDefaultValue(0);
            builder.HasOne(p => p.DirectorUser).WithMany(p => p.WorkSpaces).HasForeignKey(p => p.DirectorUserId).IsRequired();
            builder.Property(p => p.Country).HasColumnName("country").IsRequired(false);
            builder.Property(p => p.RegistrationDate).HasColumnName("registration_date").IsRequired(false);
            builder.Property(p => p.Address).HasColumnName("address").IsRequired(false);
            builder.Property(p => p.INN).HasColumnName("inn").IsRequired(false);
        }
    }
}