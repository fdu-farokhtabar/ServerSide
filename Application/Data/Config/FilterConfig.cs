using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Data.Config
{
    public class FilterConfig : IEntityTypeConfiguration<Filter>
    {
        public void Configure(EntityTypeBuilder<Filter> builder)
        {
            builder.ToTable(nameof(Filter)); //map to a real table "Filter"
            builder.HasKey(x => x.Id); // Primary Key
            builder.Property(x => x.Name).IsRequired(true).HasMaxLength(200);
            builder.Property(x => x.Tags).IsRequired(true);
            builder.Property(x => x.Groups).IsRequired(false);
            builder.Property(x => x.Order).IsRequired(true).HasDefaultValue(0);

            builder.HasIndex(x => x.Name).IsUnique(); 
            builder.HasIndex(x => x.Order);
        }
    }
}
