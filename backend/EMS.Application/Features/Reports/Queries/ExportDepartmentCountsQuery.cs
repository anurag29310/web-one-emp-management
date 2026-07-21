using MediatR;

namespace EMS.Application.Features.Reports.Queries
{
    /// <summary>Export the department headcount report as CSV.</summary>
    public class ExportDepartmentCountsQuery : IRequest<ReportExportResult> { }

    public class ReportExportResult
    {
        public byte[] Content { get; set; } = null!;
        public string ContentType { get; set; } = "text/csv";
        public string FileName { get; set; } = null!;
    }
}
