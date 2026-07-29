using EMS.Application.Features.Performance.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Performance.Queries
{
    public class GetReviewByIdQuery : IRequest<PerformanceReviewDto?>
    {
        public Guid Id { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
        public bool IsManager { get; set; }
    }
}
