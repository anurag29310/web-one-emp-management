using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    public class DeleteShiftCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
