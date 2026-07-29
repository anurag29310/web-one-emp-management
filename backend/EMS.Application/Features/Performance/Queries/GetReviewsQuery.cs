using EMS.Application.Common.DTOs;
using EMS.Application.Features.Performance.DTOs;
using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Performance.Queries
{
    public class GetReviewsQuery : IRequest<PagedResult<PerformanceReviewDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? EmployeeId { get; set; }
        public Guid? ReviewerEmployeeId { get; set; }
        public ReviewStatus? Status { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
        public bool IsManager { get; set; }
    }
}
