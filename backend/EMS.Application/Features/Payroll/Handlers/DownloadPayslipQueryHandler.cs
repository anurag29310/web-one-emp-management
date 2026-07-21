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
        private readonly IAuthRepository _authRepo;
        private readonly IFileStorageService _storage;
        private readonly ILogger<DownloadPayslipQueryHandler> _logger;

        public DownloadPayslipQueryHandler(IPayrollRepository repo, IAuthRepository authRepo, IFileStorageService storage, ILogger<DownloadPayslipQueryHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _storage = storage;
            _logger = logger;
        }

        public async Task<DownloadPayslipResult> Handle(DownloadPayslipQuery request, CancellationToken cancellationToken)
        {
            var payslip = await _repo.GetPayslipByIdAsync(request.PayslipId);
            if (payslip == null) throw new InvalidOperationException("Payslip not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != payslip.EmployeeId)
                    throw new UnauthorizedAccessException("You may only download your own payslip.");
            }

            if (string.IsNullOrEmpty(payslip.BlobContainer) || string.IsNullOrEmpty(payslip.BlobPath))
                throw new InvalidOperationException("Payslip document not found.");

            var bytes = await _storage.GetFileAsync(payslip.BlobContainer, payslip.BlobPath);
            if (bytes == null)
            {
                _logger.LogWarning("Payslip {PayslipId} has blob metadata but the file is missing from storage.", payslip.Id);
                throw new InvalidOperationException("Payslip document not found.");
            }

            _logger.LogInformation("Payslip {PayslipId} downloaded by user {UserId}.", payslip.Id, request.RequestingUserId);

            return new DownloadPayslipResult { Content = bytes, ContentType = "application/pdf", FileName = $"payslip_{payslip.EmployeeId}_{payslip.Id}.pdf" };
        }
    }
}
