using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Data.Config
{
    public class UserConfig : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable(nameof(User));
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UserName).IsRequired(true).HasMaxLength(60);
            builder.Property(x => x.Email).IsRequired(true).HasMaxLength(200);
            builder.Property(x => x.Password).IsRequired(true).HasMaxLength(250);
            builder.Property(x => x.Name).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.LastName).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.StoreName).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.Rep).IsRequired(false).HasMaxLength(500);


            builder.Property(x => x.ShippingAddress1).IsRequired(false).HasMaxLength(220);
            builder.Property(x => x.ShippingAddress2).IsRequired(false).HasMaxLength(220);
            builder.Property(x => x.ShippingCountry).IsRequired(false).HasMaxLength(50);
            builder.Property(x => x.ShippingState).IsRequired(false).HasMaxLength(50);
            builder.Property(x => x.ShippingCity).IsRequired(false).HasMaxLength(50);
            builder.Property(x => x.ShippingZipCode).IsRequired(false).HasMaxLength(50);

            builder.Property(x => x.StoreAddress1).IsRequired(false).HasMaxLength(220);
            builder.Property(x => x.StoreAddress2).IsRequired(false).HasMaxLength(220);
            builder.Property(x => x.StoreCountry).IsRequired(false).HasMaxLength(50);
            builder.Property(x => x.StoreState).IsRequired(false).HasMaxLength(50);
            builder.Property(x => x.StoreCity).IsRequired(false).HasMaxLength(50);
            builder.Property(x => x.StoreZipCode).IsRequired(false).HasMaxLength(50);

            builder.Property(x => x.TaxId).IsRequired(false).HasMaxLength(50);

            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasIndex(x => x.UserName).IsUnique();
        }
    }
}
