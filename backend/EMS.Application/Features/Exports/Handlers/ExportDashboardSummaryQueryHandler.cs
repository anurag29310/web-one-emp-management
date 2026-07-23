using EMS.Application.Common.DTOs;
using EMS.Application.Features.Exports.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Exports.Handlers
{
    public class ExportDashboardSummaryQueryHandler : IRequestHandler<ExportDashboardSummaryQuery, ExportFileResult>
    {
        private readonly IDashboardRepository _repo;
        private readonly IPdfService _pdfService;

        public ExportDashboardSummaryQueryHandler(IDashboardRepository repo, IPdfService pdfService)
        {
            _repo = repo;
            _pdfService = pdfService;
        }

        public async Task<ExportFileResult> Handle(ExportDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            var date = (request.Date ?? DateTime.UtcNow).Date;
            var summary = await _repo.GetSummaryAsync(request.DepartmentId, date, cancellationToken);

            var bytes = await _pdfService.GenerateDashboardSummaryPdfAsync(summary, date, request.DepartmentId);
            var fileName = $"dashboard-summary_{date:yyyyMMdd}.pdf";

            return new ExportFileResult
            {
                Content = bytes,
                ContentType = "application/pdf",
                FileName = fileName
            };
        }
    }
}
