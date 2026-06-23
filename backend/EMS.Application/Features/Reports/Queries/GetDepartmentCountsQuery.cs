using EMS.Application.DTOs.Reports;
using MediatR;
using System.Collections.Generic;

namespace EMS.Application.Features.Reports.Queries
{
    public class GetDepartmentCountsQuery : IRequest<IEnumerable<DepartmentCountDto>> { }
}
