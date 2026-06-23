using EMS.Application.Features.Auth;
using EMS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LoginCommandHandler _loginHandler;
        private readonly RegisterUserCommandHandler _registerHandler;
        private readonly IAuthRepository _repo;

        public AuthController(LoginCommandHandler loginHandler, RegisterUserCommandHandler registerHandler, IAuthRepository repo)
        {
            _loginHandler = loginHandler;
            _registerHandler = registerHandler;
            _repo = repo;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand cmd)
        {
            var result = await _loginHandler.Handle(cmd);
            return Ok(new { data = result });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand cmd)
        {
            var result = await _registerHandler.Handle(cmd);
            return Ok(new { data = result });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand cmd)
        {
            var token = await _repo.GetRefreshTokenAsync(cmd.RefreshToken);
            if (token == null || token.IsRevoked || token.ExpiresAtUtc <= System.DateTime.UtcNow)
                return Unauthorized();

            // create new access token
            var access = HttpContext.RequestServices.GetService(typeof(IJwtTokenService)) as IJwtTokenService;
            if (access == null) return StatusCode(500);
            var newAccess = access.GenerateAccessToken(token.User!);
            return Ok(new { data = new { accessToken = newAccess } });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutCommand cmd)
        {
            var token = await _repo.GetRefreshTokenAsync(cmd.RefreshToken);
            if (token == null) return NoContent();
            await _repo.RevokeRefreshTokenAsync(token);
            await _repo.SaveChangesAsync();
            return NoContent();
        }
    }
}
