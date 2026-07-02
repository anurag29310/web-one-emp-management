using EMS.Application.Common.DTOs;
using EMS.Application.Features.Leave.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Leave.Queries
{
    public class GetLeavesQuery : IRequest<PagedResult<LeaveRequestDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? EmployeeId { get; set; }
        public Guid? LeaveTypeId { get; set; }
        public int? Year { get; set; }
        public string? Status { get; set; }
    }
}
