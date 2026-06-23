using MediatR;
using System;

namespace EMS.Application.Features.Leave.Queries
{
    public class GetLeavesQuery : IRequest<System.Collections.Generic.IEnumerable<EMS.Domain.Entities.LeaveRequest>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? EmployeeId { get; set; }
        public Guid? LeaveTypeId { get; set; }
        public int? Year { get; set; }
        public int? Status { get; set; }
    }
}
