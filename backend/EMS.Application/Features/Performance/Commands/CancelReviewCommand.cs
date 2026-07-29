using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class CancelReviewCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? Reason { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}
