using EMS.Application.Features.Attendance.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Queries
{
    public class GetAttendanceCorrectionByIdQuery : IRequest<AttendanceCorrectionDto?>
    {
        public Guid Id { get; set; }
        public Guid RequestingUserId { get; set; }
        public bool IsAdminOrHr { get; set; }
        public bool IsManager { get; set; }
    }
}
