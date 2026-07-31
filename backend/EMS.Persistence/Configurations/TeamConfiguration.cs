using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class TeamConfiguration : IEntityTypeConfiguration<Team>
    {
        public void Configure(EntityTypeBuilder<Team> builder)
        {
            builder.ToTable("Teams");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.CompanyId).IsRequired();
            builder.Property(t => t.Name).IsRequired().HasMaxLength(150);
            builder.Property(t => t.Code).IsRequired().HasMaxLength(50);

            builder.HasIndex(t => t.CompanyId);
            builder.HasIndex(t => new { t.CompanyId, t.DepartmentId, t.Code }).IsUnique();

            builder.HasOne(t => t.Company).WithMany().HasForeignKey(t => t.CompanyId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(t => t.Department).WithMany().HasForeignKey(t => t.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(t => t.LeadEmployee).WithMany().HasForeignKey(t => t.LeadEmployeeId).OnDelete(DeleteBehavior.SetNull);

            // PostgreSQL has no automatic rowversion column; a `uint` property marked
            // IsRowVersion() is auto-mapped by the Npgsql provider to the native `xmin` system column.
            builder.Property(t => t.RowVersion).IsRowVersion();
        }
    }
}
