using EMS.API.Controllers;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace EMS.API.Middleware
{
    /// <summary>Rejects any authenticated request from a suspended/inactive company on every call,
    /// not just at login — closes the gap where a short-lived access token minted before a
    /// suspension would otherwise keep working until it expired. Runs between UseAuthentication()
    /// and UseAuthorization() so HttpContext.User is populated but a suspended tenant is stopped
    /// before reaching any policy-gated action. A missing "company_id" claim (Super Admin, or an
    /// unauthenticated/public request) is a no-op — this middleware never blocks the platform tier.</summary>
    public class TenantStatusMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantStatusMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICompanyRepository companyRepo)
        {
            var companyIdClaim = context.User?.FindFirst("company_id")?.Value;
            if (string.IsNullOrEmpty(companyIdClaim) || !Guid.TryParse(companyIdClaim, out var companyId))
            {
                await _next(context);
                return;
            }

            var company = await companyRepo.GetByIdAsync(companyId, context.RequestAborted);
            if (company == null || company.Status is not (CompanyStatus.Active or CompanyStatus.Trial))
            {
                var error = new ApiErrorResponse
                {
                    Status = (int)HttpStatusCode.Forbidden,
                    Code = "COMPANY_SUSPENDED",
                    Message = company == null
                        ? "Your company account could not be found."
                        : $"Your company's account is {company.Status}. Contact your administrator."
                };

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = error.Status;
                await context.Response.WriteAsync(JsonSerializer.Serialize(error, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
                return;
            }

            await _next(context);
        }
    }
}
