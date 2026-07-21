using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    public class UpdateEmployeeShiftCommand : IRequest<EmployeeShift>
    {
        public Guid EmployeeId { get; set; }
        public Guid AssignmentId { get; set; }
        public Guid ShiftId { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }
}
