using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Data.Config
{
    public class UserRoleConfig : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable(nameof(UserRole));
            builder.HasKey(x => x.Id);
            builder.HasOne<User>().WithMany(x => x.Roles).HasForeignKey(x => x.UserId).IsRequired(true).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).IsRequired(true).OnDelete(DeleteBehavior.Cascade);
        }

    }
}
