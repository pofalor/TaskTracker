using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Core.src.Entities;

namespace TaskTracker.DataAccess.src.EntityConfiguration
{
    public class UserConfiguration : BaseEntityConfiguration<User>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);
            builder.ToTable("User");
            builder.Property(p => p.Email).HasColumnName("email").IsRequired();
            builder.Property(p => p.FirstName).HasColumnName("first_name");
            builder.Property(p => p.LastName).HasColumnName("last_name");
            builder.Property(p => p.Country).HasColumnName("country");
        }
    }
}
