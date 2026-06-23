using EMS.Application.DTOs.Reports;
using MediatR;

namespace EMS.Application.Features.Reports.Queries
{
    public class GetEmployeeReportQuery : IRequest<EmployeeReportDto>
    {
    }
}
