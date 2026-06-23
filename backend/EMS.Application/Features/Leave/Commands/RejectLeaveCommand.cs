
namespace EMS.Application.Features.Leave.Commands
{
    public class RejectLeaveCommand 
    {
        public Guid Id { get; set; }
        public Guid ApproverId { get; set; }
        public string? Comments { get; set; }
    }
}
