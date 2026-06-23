using EMS.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace EMS.Application.Features.Departments.Handlers
{
    public class DeleteDepartmentCommandHandler
    {
        private readonly IDepartmentRepository _repo;

        public DeleteDepartmentCommandHandler(IDepartmentRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(DeleteDepartmentCommand cmd)
        {
            var dept = await _repo.GetByIdAsync(cmd.Id) ?? throw new InvalidOperationException("Department not found");
            dept.IsDeleted = true;
            dept.UpdatedAtUtc = DateTime.UtcNow;
            await _repo.DeleteAsync(dept);
            await _repo.SaveChangesAsync();
        }
    }
}
