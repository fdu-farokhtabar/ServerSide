using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Data.Config
{
    public class PoDataArchiveConfig : IEntityTypeConfiguration<PoDataArchive>
    {
        public void Configure(EntityTypeBuilder<PoDataArchive> builder)
        {
            builder.ToTable(nameof(PoDataArchive));
            builder.HasKey(x => x.PoNumber);
            builder.Property(x => x.PoNumber).HasMaxLength(15);

            builder.Property(x => x.Rep).IsRequired(false);
            builder.Property(x => x.User).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.Date).IsRequired(false);
            builder.Property(x => x.CustomerPO).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.EstimateNumber).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.Name).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.DueDate).IsRequired(false);

            builder.Property(x => x.ItemGroup).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.Forwarder).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.IOR).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.ShipTo).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.ShippingCarrier).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.ContainerNumber).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.ETAAtPort).IsRequired(false).HasMaxLength(200);

            builder.Property(x => x.FactoryStatus).IsRequired(false);
            builder.Property(x => x.StatusDate).IsRequired(false);
            builder.Property(x => x.FactoryContainerNumber).IsRequired(false).HasMaxLength(200);
            builder.Property(x => x.BookingDate).IsRequired(false);
            builder.Property(x => x.DocumentsSendOutDate).IsRequired(false);

            builder.Property(x => x.ForwarderName).IsRequired(false);
            builder.Property(x => x.FactoryBookingDate).IsRequired(false);
            builder.Property(x => x.Rate).IsRequired(false);
            builder.Property(x => x.ETD).IsRequired(false);
            builder.Property(x => x.ETA).IsRequired(false);
            builder.Property(x => x.PortOfDischarge).IsRequired(false);
            builder.Property(x => x.DischargeStatus).IsRequired(false);

            builder.Property(x => x.ShippmentStatus).IsRequired(false);
            builder.Property(x => x.ConfirmDate).IsRequired(false);

            builder.Property(x => x.GateIn).IsRequired(false);
            builder.Property(x => x.EmptyDate).IsRequired(false);
            builder.Property(x => x.GateOut).IsRequired(false);
            builder.Property(x => x.BillDate).IsRequired(false);
            builder.Property(x => x.Note).IsRequired(false);
        }
    }
}
