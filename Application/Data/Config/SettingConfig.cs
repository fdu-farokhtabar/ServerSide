using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Data.Config
{
    public class SettingConfig : IEntityTypeConfiguration<Setting>
    {
        public void Configure(EntityTypeBuilder<Setting> builder)
        {
            builder.ToTable(nameof(Setting));
            builder.HasKey(x => x.Key);
            builder.Property(x => x.Key).IsRequired(true).HasMaxLength(200);
            builder.Property(x => x.Value).IsRequired(true);
        }
    }
}
