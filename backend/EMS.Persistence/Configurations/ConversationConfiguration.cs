using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.ToTable("Conversations");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Title).HasMaxLength(250);
            builder.Property(c => c.CreatedAtUtc).IsRequired();
            builder.Property(c => c.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(c => c.LastMessageAtUtc);
            builder.HasIndex(c => c.IsDeleted);

            builder.Property(c => c.RowVersion).IsRowVersion();
        }
    }
}
