using Library.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Event).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.HasIndex(x => x.ReservationId);
    }
}
