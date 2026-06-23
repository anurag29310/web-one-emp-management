using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class GetEmployeeByIdQueryHandler : IRequestHandler<Queries.GetEmployeeByIdQuery, EMS.Domain.Entities.Employee?>
    {
        private readonly IEmployeeRepository _repo;

        public GetEmployeeByIdQueryHandler(IEmployeeRepository repo)
        {
            _repo = repo;
        }

        public async Task<EMS.Domain.Entities.Employee?> Handle(Queries.GetEmployeeByIdQuery request, CancellationToken cancellationToken)
            => await _repo.GetByIdAsync(request.Id);
    }
}
