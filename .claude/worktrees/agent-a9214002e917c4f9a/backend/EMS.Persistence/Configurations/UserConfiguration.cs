using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UserName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            // Uniqueness of UserName/Email is enforced at the application layer (scoped to
            // non-deleted rows) rather than a DB-level unique index, so a soft-deleted
            // account's username/email can be reused by a new account.
            builder.HasIndex(x => x.UserName);
            builder.HasIndex(x => x.Email);
            builder.HasMany(x => x.RefreshTokens).WithOne(x => x.User).HasForeignKey(x => x.UserId);
            builder.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId);
            builder.HasIndex(x => x.EmployeeId);
        }
    }
}
