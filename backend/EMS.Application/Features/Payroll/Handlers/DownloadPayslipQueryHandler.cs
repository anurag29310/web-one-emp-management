using EMS.Application.Interfaces;
using EMS.Application.Features.Payroll.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Payroll.Handlers
{
    public class DownloadPayslipQueryHandler : IRequestHandler<DownloadPayslipQuery, DownloadPayslipResult>
    {
        private readonly IPayrollRepository _repo;
        private readonly IFileStorageService _storage;
        private readonly ILogger<DownloadPayslipQueryHandler> _logger;

        public DownloadPayslipQueryHandler(IPayrollRepository repo, IFileStorageService storage, ILogger<DownloadPayslipQueryHandler> logger)
        {
            _repo = repo;
            _storage = storage;
            _logger = logger;
        }

        public async Task<DownloadPayslipResult> Handle(DownloadPayslipQuery request, CancellationToken cancellationToken)
        {
            var payslip = await _repo.GetPayslipByIdAsync(request.PayslipId);
            if (payslip == null) return null!;
            if (string.IsNullOrEmpty(payslip.BlobContainer) || string.IsNullOrEmpty(payslip.BlobPath)) throw new Exception("Payslip file not found");

            var bytes = await _storage.GetFileAsync(payslip.BlobContainer, payslip.BlobPath);
            if (bytes == null) throw new Exception("Payslip file missing in storage");

            return new DownloadPayslipResult { Content = bytes, ContentType = "application/pdf", FileName = $"payslip_{payslip.EmployeeId}_{payslip.Id}.pdf" };
        }
    }
}
