using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Departments
{
    public class UpdateDepartmentCommand : IRequest<Department>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public Guid? HeadEmployeeId { get; set; }
    }
}
