using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Queries
{
    public class GetShiftByIdQuery : IRequest<Shift?>
    {
        public Guid Id { get; set; }
    }
}
