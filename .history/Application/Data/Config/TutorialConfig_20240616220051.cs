using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Data.Config
{
    public class TutorialConfig : IEntityTypeConfiguration<Tutorial>
    {
        public void Configure(EntityTypeBuilder<Tutorial> builder)
        {
            builder.ToTable(nameof(Tutorial));
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Title).IsRequired(false).HasMaxLength(100);
            builder.Property(x => x.Abstract).IsRequired(false).HasMaxLength(230);
            builder.Property(x => x.Description).IsRequired(false);
            builder.Property(x => x.VideoUrls).IsRequired(false);
            builder.Property(x => x.ImageUrls).IsRequired(false);
        }
    }
}
