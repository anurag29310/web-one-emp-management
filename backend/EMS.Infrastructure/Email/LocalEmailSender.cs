using EMS.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Email
{
    public class LocalEmailSender : IEmailSender
    {
        private readonly string _outboxPath;
        private readonly ILogger<LocalEmailSender> _logger;

        public LocalEmailSender(string basePath, ILogger<LocalEmailSender> logger)
        {
            _outboxPath = Path.Combine(basePath, "Outbox");
            Directory.CreateDirectory(_outboxPath);
            _logger = logger;
        }

        public async Task SendEmailAsync(Guid userId, string subject, string body)
        {
            var file = Path.Combine(_outboxPath, $"email_{userId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.txt");
            var content = $"ToUser:{userId}\nSubject:{subject}\n\n{body}";
            await File.WriteAllTextAsync(file, content);
            _logger.LogInformation("Wrote email to outbox {File}", file);
        }
    }
}
