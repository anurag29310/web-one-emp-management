using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class CreateOfferCommand : IRequest<Guid>
    {
        public Guid CandidateId { get; set; }
        public Guid DesignationId { get; set; }
        public Guid? DepartmentId { get; set; }
        public decimal OfferedSalary { get; set; }
        public DateTime JoiningDate { get; set; }
        public string? Notes { get; set; }
    }
}
