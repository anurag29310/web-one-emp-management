using EMS.Application.Common;
using EMS.Application.Features.Reports.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reports.Handlers
{
    public class ExportDepartmentCountsQueryHandler : IRequestHandler<ExportDepartmentCountsQuery, ReportExportResult>
    {
        private readonly IReportRepository _repo;
        private readonly IExportService _exportService;

        public ExportDepartmentCountsQueryHandler(IReportRepository repo, IExportService exportService)
        {
            _repo = repo;
            _exportService = exportService;
        }

        public async Task<ReportExportResult> Handle(ExportDepartmentCountsQuery request, CancellationToken cancellationToken)
        {
            var counts = await _repo.GetDepartmentCountsAsync(cancellationToken);

            var csv = new StringBuilder();
            csv.AppendLine("DepartmentId,DepartmentName,EmployeeCount");
            foreach (var c in counts)
            {
                csv.AppendLine(string.Join(',',
                    CsvFieldFormatter.Escape(c.DepartmentId),
                    CsvFieldFormatter.Escape(c.DepartmentName),
                    CsvFieldFormatter.Escape(c.EmployeeCount)));
            }

            var fileName = $"department-counts_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            var bytes = await _exportService.GenerateCsvAsync(fileName, csv.ToString());

            return new ReportExportResult { Content = bytes, ContentType = "text/csv", FileName = fileName };
        }
    }
}
