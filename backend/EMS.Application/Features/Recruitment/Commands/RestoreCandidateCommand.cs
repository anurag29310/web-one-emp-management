using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class RestoreCandidateCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
