using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Data.Config
{
    public class ProductConfig : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable(nameof(Product));
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired(true).HasMaxLength(200);
            builder.Property(x => x.Slug).IsRequired(true).HasMaxLength(200);
            builder.Property(x => x.Inventory).IsRequired(false);
            builder.Property(x => x.ShortDescription).IsRequired(false).HasMaxLength(500);
            builder.Property(x => x.Description).IsRequired(false).HasMaxLength(2000);
            builder.Property(x => x.ProductDescription).IsRequired(false);

            builder.Property(x => x.Price).IsRequired(false).HasMaxLength(1000);
            builder.Property(x => x.Cube).IsRequired(false);
            builder.Property(x => x.W).IsRequired(false);
            builder.Property(x => x.D).IsRequired(false);
            builder.Property(x => x.H).IsRequired(false);
            builder.Property(x => x.Weight).IsRequired(false);
            builder.Property(x => x.BoxW).IsRequired(false);
            builder.Property(x => x.BoxD).IsRequired(false);
            builder.Property(x => x.BoxH).IsRequired(false);
            builder.Property(x => x.WHQTY).IsRequired(false).HasMaxLength(200);


            builder.Property(x => x.Security).IsRequired(false).HasMaxLength(2000);
            builder.Property(x => x.Order).IsRequired(true).HasDefaultValue(0);
            builder.Property(x => x.IsGroup).IsRequired(true);

            builder.Property(x => x.Tags).IsRequired(false);
            builder.Property(x => x.Groups).IsRequired(false);
            builder.Property(x => x.Factories).IsRequired(false);
            builder.Property(x => x.ComplexItemPieces).IsRequired(false);
            builder.Property(x => x.ComplexItemPriority).IsRequired();
            builder.Property(x => x.PiecesCount).IsRequired();
            builder.Property(x => x.IsSample).IsRequired(false).HasMaxLength(200);

            builder.Property(x => x.Features).IsRequired(false);
            builder.Property(x => x.PricePermissions).IsRequired(false);

            builder.HasIndex(x => x.Slug).IsUnique();
            builder.HasIndex(x => x.Order);

        }
    }
}
