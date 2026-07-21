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
    public class ExportEmployeeJoinExitQueryHandler : IRequestHandler<ExportEmployeeJoinExitQuery, ReportExportResult>
    {
        private readonly IReportRepository _repo;
        private readonly IExportService _exportService;

        public ExportEmployeeJoinExitQueryHandler(IReportRepository repo, IExportService exportService)
        {
            _repo = repo;
            _exportService = exportService;
        }

        public async Task<ReportExportResult> Handle(ExportEmployeeJoinExitQuery request, CancellationToken cancellationToken)
        {
            var records = await _repo.GetEmployeeJoinExitAsync(request.From, request.To, cancellationToken);

            var csv = new StringBuilder();
            csv.AppendLine("EmployeeId,EmployeeName,JoinDate,ExitDate");
            foreach (var r in records)
            {
                csv.AppendLine(string.Join(',',
                    CsvFieldFormatter.Escape(r.EmployeeId),
                    CsvFieldFormatter.Escape(r.EmployeeName),
                    CsvFieldFormatter.Escape(r.JoinDate),
                    CsvFieldFormatter.Escape(r.ExitDate)));
            }

            var fileName = $"employee-turnover_{request.From:yyyyMMdd}_{request.To:yyyyMMdd}.csv";
            var bytes = await _exportService.GenerateCsvAsync(fileName, csv.ToString());

            return new ReportExportResult { Content = bytes, ContentType = "text/csv", FileName = fileName };
        }
    }
}
