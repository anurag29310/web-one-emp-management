using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class AcceptOfferCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
