using MediatR;
using System;

namespace EMS.Application.Features.Payroll.Queries
{
    public class DownloadPayslipQuery : IRequest<DownloadPayslipResult>
    {
        public Guid PayslipId { get; set; }
        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }

    public class DownloadPayslipResult
    {
        public byte[] Content { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public string FileName { get; set; } = null!;
    }
}
