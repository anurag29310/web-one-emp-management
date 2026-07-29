using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class RejectOfferCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? Reason { get; set; }
    }
}
