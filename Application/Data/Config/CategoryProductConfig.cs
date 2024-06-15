using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Data.Config
{
    public class CategoryProductConfig : IEntityTypeConfiguration<CategoryProduct>
    {
        public void Configure(EntityTypeBuilder<CategoryProduct> builder)
        {
            builder.ToTable(nameof(CategoryProduct));
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CategorySlug).IsRequired(true).HasMaxLength(200);
            builder.HasOne<Product>().WithMany(x => x.Categories).HasForeignKey(x => x.ProductId).IsRequired(true).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).IsRequired(true).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
