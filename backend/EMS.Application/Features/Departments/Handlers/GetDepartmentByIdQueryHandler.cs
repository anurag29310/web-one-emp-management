using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Departments.Handlers
{
    public class GetDepartmentByIdQueryHandler : IRequestHandler<GetDepartmentByIdQuery, Department?>
    {
        private readonly IDepartmentRepository _repo;

        public GetDepartmentByIdQueryHandler(IDepartmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<Department?> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
            => await _repo.GetByIdAsync(request.Id, cancellationToken);
    }
}
