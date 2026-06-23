using EMS.Domain.Entities;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> GeneratePayslipPdfAsync(Payslip payslip);
    }
}
