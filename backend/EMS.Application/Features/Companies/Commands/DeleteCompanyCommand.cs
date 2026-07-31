using MediatR;
using System;

namespace EMS.Application.Features.Companies.Commands
{
    public class DeleteCompanyCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
