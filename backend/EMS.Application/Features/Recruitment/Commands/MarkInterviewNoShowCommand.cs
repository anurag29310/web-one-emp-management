using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class MarkInterviewNoShowCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
