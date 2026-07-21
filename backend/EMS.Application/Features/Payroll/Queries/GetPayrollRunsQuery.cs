using EMS.Application.Features.Payroll.Dtos;
using MediatR;
using System.Collections.Generic;

namespace EMS.Application.Features.Payroll.Queries
{
    public class GetPayrollRunsQuery : IRequest<IEnumerable<PayrollRunDto>> { }
}
