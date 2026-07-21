using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace EMS.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Description).HasMaxLength(250);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            // Uniqueness enforced at the application layer (scoped to non-deleted rows) so a
            // soft-deleted role's name can be reused.
            builder.HasIndex(x => x.Name);

            // These four roles are referenced by name throughout authorization policies
            // (Program.cs) and by the registration bootstrap, so they're seeded once here
            // instead of requiring manual setup in every environment.
            builder.HasData(
                new Role { Id = new Guid("11111111-1111-1111-1111-111111111111"), Name = "Admin", IsDeleted = false, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = new Guid("22222222-2222-2222-2222-222222222222"), Name = "HR", IsDeleted = false, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = new Guid("33333333-3333-3333-3333-333333333333"), Name = "Manager", IsDeleted = false, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = new Guid("44444444-4444-4444-4444-444444444444"), Name = "Employee", IsDeleted = false, CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}
