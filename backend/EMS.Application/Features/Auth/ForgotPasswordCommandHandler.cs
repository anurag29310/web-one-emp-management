using EMS.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class ForgotPasswordCommandHandler
    {
        private readonly IAuthRepository _repo;
        private readonly IEmailSender _email;

        public ForgotPasswordCommandHandler(IAuthRepository repo, IEmailSender email)
        {
            _repo = repo;
            _email = email;
        }

        public async Task Handle(ForgotPasswordCommand cmd, CancellationToken ct = default)
        {
            var user = await _repo.GetByUsernameOrEmailAsync(cmd.Email, ct);

            // Always return success to avoid user enumeration
            if (user == null || !user.IsActive)
                return;

            var resetToken = await _repo.CreatePasswordResetTokenAsync(user.Id, ct);

            var resetLink = $"https://yourapp.com/reset-password?token={resetToken}&email={cmd.Email}";

            await _email.SendEmailAsync(
                userId: user.Id,
                subject: "Reset your password",
                body: $"Click the link below to reset your password (valid for 1 hour):\n\n{resetLink}");
        }
    }
}
