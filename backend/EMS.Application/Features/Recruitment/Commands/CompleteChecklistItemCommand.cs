using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class CompleteChecklistItemCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? Notes { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }
    }
}
