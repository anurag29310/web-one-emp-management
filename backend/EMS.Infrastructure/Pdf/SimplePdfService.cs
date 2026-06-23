using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Pdf
{
    public class SimplePdfService : IPdfService
    {
        public Task<byte[]> GeneratePayslipPdfAsync(Payslip payslip)
        {
            // Minimal PDF-like placeholder (for real app replace with iTextSharp / QuestPDF)
            var sb = new StringBuilder();
            sb.AppendLine($"Payslip for Employee: {payslip.EmployeeId}");
            sb.AppendLine($"Generated At: {payslip.GeneratedAtUtc:O}");
            sb.AppendLine($"Basic: {payslip.Basic}");
            sb.AppendLine($"Allowances: {payslip.TotalAllowances}");
            sb.AppendLine($"Deductions: {payslip.TotalDeductions}");
            sb.AppendLine($"Gross: {payslip.GrossPay}");
            sb.AppendLine($"Net: {payslip.NetPay}");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return Task.FromResult(bytes);
        }
    }
}
