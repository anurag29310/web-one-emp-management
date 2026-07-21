using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    public class RejectAttendanceCorrectionCommand : IRequest
    {
        public Guid Id { get; set; }
        public Guid ApproverUserId { get; set; }
        public string? Comments { get; set; }
    }
}
