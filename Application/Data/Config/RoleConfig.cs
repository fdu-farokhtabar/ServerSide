using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Data.Config
{
    public class RoleConfig : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable(nameof(Role));
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired(true).HasMaxLength(200);
            builder.Property(x => x.Pages).IsRequired(false);
            builder.Property(x => x.Buttons).IsRequired(false);
            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
