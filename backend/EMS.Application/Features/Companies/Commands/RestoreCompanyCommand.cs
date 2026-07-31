using MediatR;
using System;

namespace EMS.Application.Features.Companies.Commands
{
    public class RestoreCompanyCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
