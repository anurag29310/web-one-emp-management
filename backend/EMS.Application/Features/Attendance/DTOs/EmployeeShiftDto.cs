using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Attendance.DTOs
{
    public class EmployeeShiftDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid ShiftId { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public static EmployeeShiftDto FromEntity(EmployeeShift es) => new()
        {
            Id = es.Id,
            EmployeeId = es.EmployeeId,
            ShiftId = es.ShiftId,
            EffectiveFrom = es.EffectiveFrom,
            EffectiveTo = es.EffectiveTo
        };
    }
}
