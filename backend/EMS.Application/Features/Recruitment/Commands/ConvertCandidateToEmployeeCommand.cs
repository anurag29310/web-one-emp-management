using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    /// <summary>Creates the real Employee record from an accepted-offer candidate. Requires an
    /// EmployeeCode and OfficeLocationId — Employee.OfficeLocationId is required and has no
    /// equivalent on Candidate/Offer, so it's supplied here at conversion time, same as
    /// CreateEmployeeCommand.</summary>
    public class ConvertCandidateToEmployeeCommand : IRequest<Guid>
    {
        public Guid CandidateId { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public Guid OfficeLocationId { get; set; }
        public Guid? TeamId { get; set; }
        public Guid? ManagerId { get; set; }
        public DateTime? JoinDate { get; set; }
    }
}
