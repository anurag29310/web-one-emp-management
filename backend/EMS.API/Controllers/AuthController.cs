using EMS.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly LoginCommandHandler _loginHandler;
        private readonly RegisterUserCommandHandler _registerHandler;
        private readonly RefreshTokenCommandHandler _refreshHandler;
        private readonly LogoutCommandHandler _logoutHandler;
        private readonly LogoutAllCommandHandler _logoutAllHandler;
        private readonly ForgotPasswordCommandHandler _forgotPasswordHandler;
        private readonly ResetPasswordCommandHandler _resetPasswordHandler;
        private readonly ChangePasswordCommandHandler _changePasswordHandler;
        private readonly GetCurrentUserQueryHandler _getCurrentUserHandler;

        public AuthController(
            LoginCommandHandler loginHandler,
            RegisterUserCommandHandler registerHandler,
            RefreshTokenCommandHandler refreshHandler,
            LogoutCommandHandler logoutHandler,
            LogoutAllCommandHandler logoutAllHandler,
            ForgotPasswordCommandHandler forgotPasswordHandler,
            ResetPasswordCommandHandler resetPasswordHandler,
            ChangePasswordCommandHandler changePasswordHandler,
            GetCurrentUserQueryHandler getCurrentUserHandler)
        {
            _loginHandler = loginHandler;
            _registerHandler = registerHandler;
            _refreshHandler = refreshHandler;
            _logoutHandler = logoutHandler;
            _logoutAllHandler = logoutAllHandler;
            _forgotPasswordHandler = forgotPasswordHandler;
            _resetPasswordHandler = resetPasswordHandler;
            _changePasswordHandler = changePasswordHandler;
            _getCurrentUserHandler = getCurrentUserHandler;
        }

        /// <summary>Authenticate with username/email and password.</summary>
        [AllowAnonymous]
        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResult>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 401)]
        [ProducesResponseType(typeof(ApiErrorResponse), 429)]
        public async Task<IActionResult> Login([FromBody] LoginCommand cmd, CancellationToken ct)
        {
            var result = await _loginHandler.Handle(cmd, ct);
            return Ok(ApiResponse<LoginResult>.Success(result));
        }

        /// <summary>Register a new user account.</summary>
        [AllowAnonymous]
        [EnableRateLimiting("RegisterPolicy")]
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<LoginResult>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(typeof(ApiErrorResponse), 429)]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand cmd, CancellationToken ct)
        {
            var result = await _registerHandler.Handle(cmd, ct);
            return StatusCode(201, ApiResponse<LoginResult>.Success(result, "Account created successfully."));
        }

        /// <summary>Exchange a valid refresh token for a new access token and rotated refresh token.</summary>
        [AllowAnonymous]
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(ApiResponse<RefreshTokenResult>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 401)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand cmd, CancellationToken ct)
        {
            var result = await _refreshHandler.Handle(cmd, ct);
            return Ok(ApiResponse<RefreshTokenResult>.Success(result));
        }

        /// <summary>Revoke the supplied refresh token (logout current session).</summary>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> Logout([FromBody] LogoutCommand cmd, CancellationToken ct)
        {
            await _logoutHandler.Handle(cmd, ct);
            return NoContent();
        }

        /// <summary>Revoke all active refresh tokens for the current user (logout all sessions).</summary>
        [Authorize]
        [HttpPost("logout-all")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> LogoutAll(CancellationToken ct)
        {
            var cmd = new LogoutAllCommand { UserId = GetCurrentUserId() };
            await _logoutAllHandler.Handle(cmd, ct);
            return NoContent();
        }

        /// <summary>Request a password reset email. Always returns 204 to prevent user enumeration.</summary>
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand cmd, CancellationToken ct)
        {
            await _forgotPasswordHandler.Handle(cmd, ct);
            return NoContent();
        }

        /// <summary>Reset password using the token received via email.</summary>
        [AllowAnonymous]
        [HttpPost("reset-password")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand cmd, CancellationToken ct)
        {
            await _resetPasswordHandler.Handle(cmd, ct);
            return NoContent();
        }

        /// <summary>Change password for the currently authenticated user.</summary>
        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(typeof(ApiErrorResponse), 401)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand cmd, CancellationToken ct)
        {
            cmd.UserId = GetCurrentUserId();
            await _changePasswordHandler.Handle(cmd, ct);
            return NoContent();
        }

        /// <summary>Get the profile of the currently authenticated user.</summary>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<CurrentUserDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 401)]
        public async Task<IActionResult> Me(CancellationToken ct)
        {
            var query = new GetCurrentUserQuery { UserId = GetCurrentUserId() };
            var result = await _getCurrentUserHandler.Handle(query, ct);
            return Ok(ApiResponse<CurrentUserDto>.Success(result));
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("User identity could not be resolved.");

            return id;
        }
    }

    // ─── Shared response envelopes ──────────────────────────────────────────────

    public class ApiResponse<T>
    {
        public T Data { get; set; } = default!;
        public string Message { get; set; } = "Request completed successfully.";
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N")[..16];

        public static ApiResponse<T> Success(T data, string message = "Request completed successfully.") =>
            new() { Data = data, Message = message };
    }

    public class ApiErrorResponse
    {
        public int Status { get; set; }
        public string Code { get; set; } = null!;
        public string Message { get; set; } = null!;
        public object? Errors { get; set; }
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N")[..16];
    }
}
