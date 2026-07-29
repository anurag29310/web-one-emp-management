using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class SubmitManagerReviewCommand : IRequest
    {
        public Guid Id { get; set; }
        public string ManagerAssessment { get; set; } = null!;
        public decimal OverallRating { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}
