using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    public class DeleteAttendanceRecordCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
