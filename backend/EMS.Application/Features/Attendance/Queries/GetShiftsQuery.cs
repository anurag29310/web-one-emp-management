using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace EMS.Application.Features.Attendance.Queries
{
    public class GetShiftsQuery : IRequest<IEnumerable<Shift>>
    {
    }
}
