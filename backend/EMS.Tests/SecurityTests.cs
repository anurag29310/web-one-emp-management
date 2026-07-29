using EMS.Domain.Entities;
using EMS.Infrastructure.Services;
using EMS.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    /// <summary>
    /// Cross-module authorization sweep: instead of hand-picking a handful of endpoints from each
    /// module (the existing per-module test files already cover their own handlers this way),
    /// this reflects over every controller action in EMS.API and, for every one that declares a
    /// named policy or role restriction (at the method or, failing that, the controller level),
    /// verifies a plain "Employee" role caller is rejected — automatically covering every current
    /// endpoint and any future one, without needing to update this file when a new controller ships.
    /// </summary>
    [Collection("WebApplicationFactory")]
    public class SecurityTests
    {
        private const string JwtKey = "this-test-signing-key-is-at-least-32-bytes-long!";
        private const string JwtIssuer = "ems-test";

        private static WebApplicationFactory<Program> CreateFactory()
        {
            Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
            Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);

            var dbName = "SecurityTestDb_" + Guid.NewGuid();

            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                    services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));
                });
            });
        }

        // No database round-trip needed: JWT bearer auth reads role/identity straight from the
        // token's claims (see JwtTokenService), never looking the user up again per request, so a
        // synthetic in-memory User is exactly as valid as one fetched from a real login.
        private static string MintToken(string roleName)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = JwtIssuer
            }).Build();
            var jwtService = new JwtTokenService(config);

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = roleName.ToLowerInvariant() + "-test",
                Email = $"{roleName.ToLowerInvariant()}@test.local",
                PasswordHash = "x",
                Role = new Role { Id = Guid.NewGuid(), Name = roleName }
            };
            return jwtService.GenerateAccessToken(user);
        }

        private sealed record DiscoveredEndpoint(HttpMethod Method, string Route, string Description, bool RequiresMultipart);

        // Picks whichever restriction actually governs the action: the method's own [Authorize],
        // or (if the method has none) the controller's class-level [Authorize] — matching ASP.NET
        // Core's real resolution closely enough for this purpose, since Employee already fails every
        // currently-defined policy/role list regardless of which level supplies the requirement.
        // Plain [Authorize] with neither Policy nor Roles set (any authenticated user) is not a
        // privilege restriction, so those actions are excluded from this sweep — an Employee caller
        // is expected to reach them (self-scoping, not role, governs what they see there).
        private static AuthorizeAttribute? EffectiveAuthorize(Type controllerType, MethodInfo method)
        {
            if (method.GetCustomAttribute<AllowAnonymousAttribute>() != null) return null;

            var methodAuth = method.GetCustomAttribute<AuthorizeAttribute>();
            if (methodAuth != null && (methodAuth.Policy != null || methodAuth.Roles != null)) return methodAuth;

            var classAuth = controllerType.GetCustomAttribute<AuthorizeAttribute>();
            if (classAuth != null && (classAuth.Policy != null || classAuth.Roles != null)) return classAuth;

            return null;
        }

        private static IReadOnlyList<DiscoveredEndpoint> DiscoverPolicyGatedEndpoints()
        {
            var controllerTypes = typeof(Program).Assembly.GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && t.IsPublic && !t.IsAbstract);

            var endpoints = new List<DiscoveredEndpoint>();

            foreach (var controllerType in controllerTypes)
            {
                var classRoute = controllerType.GetCustomAttribute<RouteAttribute>()?.Template;
                if (string.IsNullOrEmpty(classRoute)) continue;

                foreach (var method in controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var auth = EffectiveAuthorize(controllerType, method);
                    if (auth == null) continue;

                    var httpAttr = method.GetCustomAttributes()
                        .OfType<IActionHttpMethodProvider>()
                        .FirstOrDefault();
                    if (httpAttr == null) continue;

                    var verb = httpAttr.HttpMethods.First();
                    var methodRoute = (httpAttr as IRouteTemplateProvider)?.Template;

                    string fullRoute;
                    if (methodRoute != null && methodRoute.StartsWith("~/"))
                    {
                        // "~/" makes the method's route absolute, replacing the controller's [Route]
                        // prefix entirely (e.g. ShiftController exposing routes under /api/v1/employees).
                        fullRoute = "/" + methodRoute[2..].Trim('/');
                    }
                    else
                    {
                        fullRoute = "/" + classRoute.Trim('/');
                        if (!string.IsNullOrEmpty(methodRoute))
                            fullRoute += "/" + methodRoute.Trim('/');
                    }

                    // Conventional routing token, e.g. [Route("api/[controller]")] on
                    // AnnouncementsController → "api/Announcements".
                    var controllerName = controllerType.Name.EndsWith("Controller", StringComparison.Ordinal)
                        ? controllerType.Name[..^"Controller".Length]
                        : controllerType.Name;
                    fullRoute = fullRoute.Replace("[controller]", controllerName, StringComparison.OrdinalIgnoreCase);

                    // Route params ({id}, {id:guid}, {employeeId}, ...) → a fresh Guid. Authorization
                    // middleware runs before model binding/routing constraints are evaluated against
                    // real data, so any well-formed placeholder reaches the [Authorize] check.
                    fullRoute = Regex.Replace(fullRoute, @"\{[^}]+\}", _ => Guid.NewGuid().ToString());

                    // File-upload actions bind an IFormFile parameter, which requires
                    // multipart/form-data — sending JSON there gets rejected as 415 by content
                    // negotiation before authorization even runs, which would otherwise look like a
                    // false failure below.
                    var requiresMultipart = method.GetParameters().Any(p => p.ParameterType == typeof(IFormFile));

                    var description = $"{controllerType.Name}.{method.Name} [{verb} {fullRoute}] (policy: {auth.Policy}, roles: {auth.Roles})";
                    endpoints.Add(new DiscoveredEndpoint(new HttpMethod(verb), fullRoute, description, requiresMultipart));
                }
            }

            return endpoints;
        }

        private static HttpContent? BuildBody(DiscoveredEndpoint endpoint)
        {
            if (endpoint.Method != HttpMethod.Post && endpoint.Method != HttpMethod.Put && endpoint.Method.Method != "PATCH")
                return null;

            if (endpoint.RequiresMultipart)
                return new MultipartFormDataContent();

            return new StringContent("{}", Encoding.UTF8, "application/json");
        }

        private static HttpRequestMessage BuildRequest(DiscoveredEndpoint endpoint, string token)
        {
            var request = new HttpRequestMessage(endpoint.Method, endpoint.Route);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = BuildBody(endpoint);
            return request;
        }

        [Fact]
        public void DiscoverPolicyGatedEndpoints_FindsASubstantialSet()
        {
            // Sanity check on the discovery mechanism itself: if this ever drops near zero, the
            // reflection logic broke (e.g. a controller/attribute pattern it doesn't recognize
            // anymore), which would make every other test in this file vacuously pass without
            // testing anything. At last count there were 30+ across Employees/Departments/Leave/
            // Attendance/Payroll/Clients/Tasks/Reimbursements/Users/Roles/AuditLogs/etc.
            var endpoints = DiscoverPolicyGatedEndpoints();
            Assert.True(endpoints.Count >= 20, $"Expected at least 20 policy-gated endpoints via reflection, found {endpoints.Count}. The discovery logic may no longer match the controllers' attribute usage.");
        }

        [Fact]
        public async Task EveryPolicyGatedEndpoint_RejectsPlainEmployeeCaller()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var employeeToken = MintToken("Employee");

            var endpoints = DiscoverPolicyGatedEndpoints();
            var failures = new List<string>();

            foreach (var endpoint in endpoints)
            {
                using var request = BuildRequest(endpoint, employeeToken);
                using var response = await client.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.Forbidden)
                    failures.Add($"{endpoint.Description} -> expected 403, got {(int)response.StatusCode}");
            }

            Assert.True(failures.Count == 0,
                $"{failures.Count} of {endpoints.Count} policy-gated endpoint(s) did NOT reject a plain Employee-role caller:\n" + string.Join("\n", failures));
        }

        [Fact]
        public async Task EveryPolicyGatedEndpoint_RejectsUnauthenticatedCaller()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();

            var endpoints = DiscoverPolicyGatedEndpoints();
            var failures = new List<string>();

            foreach (var endpoint in endpoints)
            {
                var request = new HttpRequestMessage(endpoint.Method, endpoint.Route);
                request.Content = BuildBody(endpoint);

                using var response = await client.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                    failures.Add($"{endpoint.Description} -> expected 401 with no token, got {(int)response.StatusCode}");
            }

            Assert.True(failures.Count == 0,
                $"{failures.Count} of {endpoints.Count} policy-gated endpoint(s) did NOT reject an unauthenticated caller:\n" + string.Join("\n", failures));
        }

        [Fact]
        public async Task ApprovePayrollRun_RejectsHrEvenThoughHrCanManagePayroll()
        {
            // CanManagePayroll allows Admin or HR; CanApprovePayroll is Admin-only. HR being able to
            // run payroll but not approve it is a real, easy-to-get-backwards distinction — this
            // pins it down explicitly rather than relying on the blanket Employee sweep above (HR
            // already passes the *other* payroll policy, so this is the case most likely to regress
            // silently if someone "simplifies" the two policies into one).
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var hrToken = MintToken("HR");

            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/payroll/runs/{Guid.NewGuid()}/approve");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", hrToken);

            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AdminCaller_IsNotBlockedByAuthorization_OnRepresentativeEndpoints()
        {
            // Guards against the sweep above passing for the wrong reason (e.g. a JWT
            // misconfiguration that rejects every caller, Admin included, which would make
            // EveryPolicyGatedEndpoint_RejectsPlainEmployeeCaller pass vacuously). Admin may still
            // get 400/404/409 from these dummy requests — anything except 401/403 proves
            // authorization itself let the call through.
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var adminToken = MintToken("Admin");

            var representative = new[]
            {
                new HttpRequestMessage(HttpMethod.Get, "/api/v1/clients"),
                new HttpRequestMessage(HttpMethod.Get, "/api/v1/tasks"),
                new HttpRequestMessage(HttpMethod.Get, "/api/v1/payroll/salary-structures"),
                new HttpRequestMessage(HttpMethod.Get, "/api/v1/audit-logs"),
                new HttpRequestMessage(HttpMethod.Get, "/api/v1/users"),
            };

            foreach (var request in representative)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
                using var response = await client.SendAsync(request);
                Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
            }
        }
    }
}
