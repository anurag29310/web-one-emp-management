using System.ComponentModel.DataAnnotations;

namespace EMS.Application.Features.Auth
{
    public class LoginCommand
    {
        public string UserNameOrEmail { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
