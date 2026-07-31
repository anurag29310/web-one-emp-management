using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class GetEmployeeByIdQueryHandler : IRequestHandler<Queries.GetEmployeeByIdQuery, EMS.Domain.Entities.Employee?>
    {
        private readonly IEmployeeRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public GetEmployeeByIdQueryHandler(IEmployeeRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<EMS.Domain.Entities.Employee?> Handle(Queries.GetEmployeeByIdQuery request, CancellationToken cancellationToken)
        {
            var emp = await _repo.GetByIdAsync(request.Id, cancellationToken);
            return emp != null && emp.CompanyId == _currentUser.CompanyId!.Value ? emp : null;
        }
    }
}
