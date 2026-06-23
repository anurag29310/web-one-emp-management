using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace EMS.Application.Features.Departments.Handlers
{
    public class UpdateDepartmentCommandHandler
    {
        private readonly IDepartmentRepository _repo;

        public UpdateDepartmentCommandHandler(IDepartmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<Department> Handle(UpdateDepartmentCommand cmd)
        {
            var dept = await _repo.GetByIdAsync(cmd.Id) ?? throw new InvalidOperationException("Department not found");
            dept.Name = cmd.Name;
            dept.Code = cmd.Code;
            dept.Description = cmd.Description;
            dept.HeadEmployeeId = cmd.HeadEmployeeId;
            dept.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(dept);
            await _repo.SaveChangesAsync();
            return dept;
        }
    }
}
