using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Body).IsRequired().HasMaxLength(4000);
            builder.Property(m => m.SentAtUtc).IsRequired();

            builder.HasIndex(m => new { m.ConversationId, m.SentAtUtc });
            builder.HasIndex(m => m.SenderUserId);

            builder.HasOne<Conversation>().WithMany().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
