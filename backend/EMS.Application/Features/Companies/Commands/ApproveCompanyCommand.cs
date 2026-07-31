using MediatR;
using System;

namespace EMS.Application.Features.Companies.Commands
{
    public class ApproveCompanyCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
