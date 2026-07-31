using MediatR;
using System;

namespace EMS.Application.Features.Companies.Commands
{
    /// <summary>Revokes every refresh token for a company's users without touching its Status —
    /// for on-demand use (e.g. a suspected compromised account) separate from SuspendCompanyCommand,
    /// which does the same thing as a side effect of suspending.</summary>
    public class ForceLogoutCompanyCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
