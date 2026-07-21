using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Leave.Handlers
{
    public class GetLeaveBalancesQueryHandler : IRequestHandler<Queries.GetLeaveBalancesQuery, IEnumerable<EMS.Domain.Entities.LeaveBalance>>
    {
        private readonly ILeaveRepository _repo;
        private readonly IAuthRepository _authRepo;

        public GetLeaveBalancesQueryHandler(ILeaveRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        public async Task<IEnumerable<EMS.Domain.Entities.LeaveBalance>> Handle(Queries.GetLeaveBalancesQuery request, CancellationToken cancellationToken)
        {
            var employeeId = request.EmployeeId;

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null)
                    return Enumerable.Empty<EMS.Domain.Entities.LeaveBalance>();

                if (employeeId != Guid.Empty && employeeId != requester.EmployeeId.Value)
                    throw new UnauthorizedAccessException("You may only view your own leave balances.");

                employeeId = requester.EmployeeId.Value;
            }

            return await _repo.GetLeaveBalancesForEmployeeAsync(employeeId, cancellationToken);
        }
    }
}
