using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class CancelInterviewCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
