using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace EMS.Application.Features.Departments.Handlers
{
    public class CreateDepartmentCommandHandler
    {
        private readonly IDepartmentRepository _repo;

        public CreateDepartmentCommandHandler(IDepartmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<Department> Handle(CreateDepartmentCommand cmd)
        {
            var dept = new Department
            {
                Id = Guid.NewGuid(),
                Name = cmd.Name,
                Code = cmd.Code,
                Description = cmd.Description,
                HeadEmployeeId = cmd.HeadEmployeeId,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            await _repo.AddAsync(dept);
            await _repo.SaveChangesAsync();
            return dept;
        }
    }
}
