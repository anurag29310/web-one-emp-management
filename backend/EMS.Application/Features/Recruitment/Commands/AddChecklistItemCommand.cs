using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class AddChecklistItemCommand : IRequest<Guid>
    {
        public Guid CandidateId { get; set; }
        public string ItemName { get; set; } = null!;
        public string? Notes { get; set; }
    }
}
