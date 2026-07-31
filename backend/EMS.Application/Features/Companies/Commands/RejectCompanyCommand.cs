using MediatR;
using System;

namespace EMS.Application.Features.Companies.Commands
{
    public class RejectCompanyCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? Reason { get; set; }
    }
}
