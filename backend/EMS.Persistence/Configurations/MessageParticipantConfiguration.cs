using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class MessageParticipantConfiguration : IEntityTypeConfiguration<MessageParticipant>
    {
        public void Configure(EntityTypeBuilder<MessageParticipant> builder)
        {
            builder.ToTable("MessageParticipants");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.JoinedAtUtc).IsRequired();

            builder.HasIndex(p => new { p.ConversationId, p.UserId }).IsUnique();
            builder.HasIndex(p => p.UserId);

            builder.HasOne<Conversation>().WithMany().HasForeignKey(p => p.ConversationId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
