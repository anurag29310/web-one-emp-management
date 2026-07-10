using EMS.API.Controllers;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace EMS.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleAsync(context, ex);
            }
        }

        private async Task HandleAsync(HttpContext context, Exception ex)
        {
            var error = ex switch
            {
                ValidationException validationEx => new ApiErrorResponse
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred.",
                    Errors = validationEx.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
                },
                InvalidOperationException notFoundEx when notFoundEx.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) => new ApiErrorResponse
                {
                    Status = (int)HttpStatusCode.NotFound,
                    Code = "NOT_FOUND",
                    Message = notFoundEx.Message
                },
                InvalidOperationException conflictEx => new ApiErrorResponse
                {
                    Status = (int)HttpStatusCode.Conflict,
                    Code = "CONFLICT",
                    Message = conflictEx.Message
                },
                UnauthorizedAccessException => new ApiErrorResponse
                {
                    Status = (int)HttpStatusCode.Forbidden,
                    Code = "FORBIDDEN",
                    Message = "You do not have permission to perform this action."
                },
                _ => new ApiErrorResponse
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Code = "INTERNAL_ERROR",
                    Message = "An unexpected error occurred. Please try again later."
                }
            };

            if (error.Status == (int)HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning(ex, "Handled exception ({Code}) processing {Method} {Path}", error.Code, context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = error.Status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(error, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
