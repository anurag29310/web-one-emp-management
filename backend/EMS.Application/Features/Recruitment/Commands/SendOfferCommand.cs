using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class SendOfferCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
