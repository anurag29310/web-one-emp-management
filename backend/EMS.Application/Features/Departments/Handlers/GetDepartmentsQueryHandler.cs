using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Features.Departments.Handlers
{
    public class GetDepartmentsQueryHandler
    {
        private readonly IDepartmentRepository _repo;

        public GetDepartmentsQueryHandler(IDepartmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Department>> Handle(GetDepartmentsQuery query)
            => await _repo.GetAllAsync();
    }
}
