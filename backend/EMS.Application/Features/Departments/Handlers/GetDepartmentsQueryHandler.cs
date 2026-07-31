using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Departments.Handlers
{
    public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, IEnumerable<Department>>
    {
        private readonly IDepartmentRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public GetDepartmentsQueryHandler(IDepartmentRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<Department>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
            => await _repo.GetAllAsync(_currentUser.CompanyId!.Value, cancellationToken);
    }
}
