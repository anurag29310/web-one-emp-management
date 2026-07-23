using System;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> GeneratePayslipPdfAsync(PayslipDocument document);

        Task<byte[]> GenerateDashboardSummaryPdfAsync(EMS.Application.DTOs.DashboardSummaryDto summary, DateTime asOfDate, Guid? departmentId);
    }
}
