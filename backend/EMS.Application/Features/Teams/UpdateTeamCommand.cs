using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Teams
{
    public class UpdateTeamCommand : IRequest<Team>
    {
        public Guid Id { get; set; }
        public Guid DepartmentId { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public Guid? LeadEmployeeId { get; set; }
    }
}
