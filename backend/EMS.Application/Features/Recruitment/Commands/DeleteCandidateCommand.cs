using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class DeleteCandidateCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}
