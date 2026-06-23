using MediatR;
using System.Collections.Generic;
using EMS.Application.Features.Payroll.Dtos;

namespace EMS.Application.Features.Payroll.Queries
{
    public class GetSalaryStructuresQuery : IRequest<IEnumerable<SalaryStructureDto>> { }
}
