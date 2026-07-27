using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class ReimbursementConfiguration : IEntityTypeConfiguration<Reimbursement>
    {
        public void Configure(EntityTypeBuilder<Reimbursement> builder)
        {
            builder.ToTable("Reimbursements");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.ReimbursementNumber).IsRequired().HasMaxLength(20);
            builder.Property(r => r.ExpenseTitle).IsRequired().HasMaxLength(200);
            builder.Property(r => r.ExpenseCategory).IsRequired().HasMaxLength(100);
            builder.Property(r => r.Amount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(r => r.Currency).IsRequired().HasMaxLength(10);
            builder.Property(r => r.Description).HasMaxLength(2000);
            builder.Property(r => r.Notes).HasMaxLength(1000);
            builder.Property(r => r.ReviewRemarks).HasMaxLength(1000);
            builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.HasIndex(r => r.ReimbursementNumber).IsUnique();
            builder.HasIndex(r => r.EmployeeId);
            builder.HasIndex(r => r.Status);
            builder.HasIndex(r => new { r.EmployeeId, r.Status, r.PayrollProcessed });

            builder.HasOne(r => r.Employee).WithMany().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(r => r.RowVersion).IsRowVersion();
        }
    }
}
