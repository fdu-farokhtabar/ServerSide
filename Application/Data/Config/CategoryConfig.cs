using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Application.Data.Config
{
    public class CategoryConfig : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable(nameof(Category));
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired(true).HasMaxLength(200);
            builder.Property(x => x.Slug).IsRequired(true).HasMaxLength(200);
            builder.Property(x => x.ShortDescription).IsRequired(false).HasMaxLength(500);
            builder.Property(x => x.Description).IsRequired(false).HasMaxLength(2000);
            builder.Property(x => x.Parameter).IsRequired(false).HasMaxLength(5000);
            builder.Property(x => x.Order).IsRequired(true).HasDefaultValue(0);
            builder.Property(x => x.PublishedCatalogType).IsRequired(true);
            builder.Property(x => x.Tags).IsRequired(false).HasMaxLength(2000);
            builder.Property(x => x.Security).IsRequired(false).HasMaxLength(2000);

            builder.HasIndex(x => x.Slug).IsUnique();
            builder.HasIndex(x => x.Order);
        }
    }
}
