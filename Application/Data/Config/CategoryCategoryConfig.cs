using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Data.Config
{
    public class CategoryCategoryConfig : IEntityTypeConfiguration<CategoryCategory>
    {
        public void Configure(EntityTypeBuilder<CategoryCategory> builder)
        {
            builder.ToTable(nameof(CategoryCategory));
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CategorySlug).IsRequired(true).HasMaxLength(200);
            builder.Property(x => x.ParentCategorySlug).IsRequired(true).HasMaxLength(200);
            builder.Property(x => x.Order).IsRequired(true).HasDefaultValue(0);
            builder.Property(x => x.CategoryId).IsRequired(true);
            builder.Property(x => x.ParentCategoryId).IsRequired(true);
            builder.HasOne<Category>().WithMany(x => x.Parents).HasForeignKey(x => x.CategoryId).IsRequired(true).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Category>().WithMany().HasForeignKey(x => x.ParentCategoryId).IsRequired(true).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
