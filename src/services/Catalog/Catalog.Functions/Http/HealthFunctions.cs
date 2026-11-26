using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Catalog.Functions.Http;

/// <summary>GET /api/catalog/health: inclui o check do banco que a integração do Aspire registra.</summary>
public sealed class HealthFunctions(HealthCheckService health)
{
    [Function("CatalogHealth")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "catalog/health")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var report = await health.CheckHealthAsync(cancellationToken);
        return new ContentResult
        {
            Content = report.Status.ToString(),
            ContentType = "text/plain",
            StatusCode = report.Status == HealthStatus.Healthy
                ? StatusCodes.Status200OK
                : StatusCodes.Status503ServiceUnavailable,
        };
    }
}
