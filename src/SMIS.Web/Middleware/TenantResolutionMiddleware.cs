using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Platform;
using SMIS.Infrastructure.Data;
using SMIS.Web.Services;

namespace SMIS.Web.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, MasterDbContext masterDb)
    {
        var tenantDbClaim = context.User.FindFirst("TenantDb")?.Value;

        if (!string.IsNullOrEmpty(tenantDbClaim))
        {
            // Already signed in — re-check the school's current status on
            // every request, so a mid-session suspension takes effect
            // immediately rather than only at next login.
            var reg = await masterDb.SchoolRegistrations
                .FirstOrDefaultAsync(r => r.DatabaseName == tenantDbClaim);

            if (reg == null || reg.Status != RegistrationStatus.Approved)
            {
                await context.SignOutAsync(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);

                var slug = reg?.Slug;
                var redirectUrl = !string.IsNullOrEmpty(slug)
                    ? $"/{slug}/Login?suspended=true"
                    : "/Account/Login";

                context.Response.Redirect(redirectUrl);
                return; // short-circuit — do not call _next
            }

            tenantContext.DatabaseName = reg.DatabaseName;
            tenantContext.SchoolName = reg.SchoolName;
        }
        else
        {
            // Not signed in yet — resolving for a /{slug}/Login request
            var routeValues = context.GetRouteData()?.Values;
            if (routeValues != null && routeValues.TryGetValue("slug", out var slugObj) && slugObj is string slug)
            {
                var reg = await masterDb.SchoolRegistrations
                    .FirstOrDefaultAsync(r => r.Slug == slug.ToLowerInvariant());

                if (reg == null)
                {
                    tenantContext.SlugNotFound = true;
                }
                else if (reg.Status == RegistrationStatus.Suspended)
                {
                    tenantContext.IsSuspended = true;
                    tenantContext.SchoolName = reg.SchoolName;
                }
                else if (reg.Status == RegistrationStatus.Approved)
                {
                    tenantContext.DatabaseName = reg.DatabaseName;
                    tenantContext.SchoolName = reg.SchoolName;
                }
                else
                {
                    // Pending or Rejected — treat the same as "not found"
                    // so it doesn't leak status detail to the public.
                    tenantContext.SlugNotFound = true;
                }
            }
        }

        await _next(context);
    }
}