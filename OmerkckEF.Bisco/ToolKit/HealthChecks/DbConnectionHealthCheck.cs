using Microsoft.Extensions.Diagnostics.HealthChecks;
using OmerkckEF.Biscom.DBContext;
using System.Data.Common;

namespace OmerkckEF.Biscom.ToolKit.HealthChecks
{
    public class DbConnectionHealthCheck(EntityContext dbContext) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // In a real scenario, we would use a simple "SELECT 1" to check connection
                // Here we just try to open it via the existing logic
                
                var result = await dbContext.RunScalerAsync("SELECT 1");
                
                if (result.IsSuccess)
                {
                    return HealthCheckResult.Healthy("Database connection is working.");
                }

                return HealthCheckResult.Unhealthy($"Database connection failed: {result.Message}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database health check failed with exception.", ex);
            }
        }
    }
}
