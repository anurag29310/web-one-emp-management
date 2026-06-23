using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System.Threading.Tasks;

namespace EMS.Application.Features.Departments.Handlers
{
    public class GetDepartmentByIdQueryHandler
    {
        private readonly IDepartmentRepository _repo;

        public GetDepartmentByIdQueryHandler(IDepartmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<Department?> Handle(GetDepartmentByIdQuery query)
            => await _repo.GetByIdAsync(query.Id);
    }
}
