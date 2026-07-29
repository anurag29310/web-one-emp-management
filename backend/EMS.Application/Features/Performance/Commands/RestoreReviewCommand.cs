using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class RestoreReviewCommand : IRequest
    {
        public Guid Id { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}
