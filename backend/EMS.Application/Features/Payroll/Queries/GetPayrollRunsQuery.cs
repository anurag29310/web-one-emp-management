using MediatR;
using System.Collections.Generic;
using EMS.Domain.Entities;

namespace EMS.Application.Features.Payroll.Queries
{
    public class GetPayrollRunsQuery : IRequest<IEnumerable<PayrollRun>> { }
}
