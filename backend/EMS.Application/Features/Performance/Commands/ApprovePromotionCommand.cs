using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class ApprovePromotionCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? DecisionNotes { get; set; }
        public Guid RequestingUserId { get; set; }
    }
}
