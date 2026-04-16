using Hangfire.Dashboard;

namespace Boilerate.Infrastructure.BackgroundJobs;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext.Request.Host.Host.Contains("localhost"))
        {
            return true;
        }

        return httpContext.User.Identity?.IsAuthenticated ?? false;
    }
}
