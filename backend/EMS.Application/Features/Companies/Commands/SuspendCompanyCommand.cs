using MediatR;
using System;

namespace EMS.Application.Features.Companies.Commands
{
    /// <summary>Suspending a company immediately force-logs-out every one of its users — see
    /// SuspendCompanyCommandHandler.</summary>
    public class SuspendCompanyCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? Reason { get; set; }
    }
}
