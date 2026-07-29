using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class CreateCandidateCommand : IRequest<Guid>
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public Guid DesignationId { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? Source { get; set; }
        public DateTime AppliedDate { get; set; }
        public string? Notes { get; set; }
    }
}
