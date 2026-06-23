using System;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(Guid userId, string subject, string body);
    }
}
