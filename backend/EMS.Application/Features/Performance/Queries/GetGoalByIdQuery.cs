using EMS.Application.Features.Performance.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Performance.Queries
{
    public class GetGoalByIdQuery : IRequest<PerformanceGoalDto?>
    {
        public Guid Id { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
        public bool IsManager { get; set; }
    }
}
