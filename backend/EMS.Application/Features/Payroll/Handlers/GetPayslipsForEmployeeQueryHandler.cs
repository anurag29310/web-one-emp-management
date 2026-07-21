using EMS.Application.Features.Payroll.Dtos;
using EMS.Application.Features.Payroll.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class GetPayslipsForEmployeeQueryHandler : IRequestHandler<GetPayslipsForEmployeeQuery, IEnumerable<PayslipDto>>
    {
        private readonly IPayrollRepository _repo;
        private readonly IAuthRepository _authRepo;

        public GetPayslipsForEmployeeQueryHandler(IPayrollRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        public async Task<IEnumerable<PayslipDto>> Handle(GetPayslipsForEmployeeQuery request, CancellationToken cancellationToken)
        {
            var employeeId = request.EmployeeId;

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null)
                    return Enumerable.Empty<PayslipDto>();

                if (employeeId.HasValue && employeeId.Value != requester.EmployeeId.Value)
                    throw new UnauthorizedAccessException("You may only view your own payslips.");

                // Non-privileged callers are always scoped to their own employee record,
                // regardless of any employeeId filter supplied on the query string.
                employeeId = requester.EmployeeId;
            }

            var payslips = await _repo.GetPayslipsForEmployeeAsync(employeeId!.Value);
            return payslips.Select(PayslipDto.FromEntity).ToList();
        }
    }
}
