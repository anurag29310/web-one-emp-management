using System;

namespace EMS.Application.Features.Departments
{
    public class CreateDepartmentCommand
    {
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public Guid? HeadEmployeeId { get; set; }
    }
}
