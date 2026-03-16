using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmerkckEF.Biscom.DBContext;
using OmerkckEF.Biscom.Interfaces;
using OmerkckEF.Biscom.ToolKit;
using OmerkckEF.Biscom.ToolKit.HealthChecks;
using Serilog;

namespace OmerkckEF.Biscom
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOmerkckEF(this IServiceCollection services, Action<DBServer>? configure = null)
        {
            var dbServer = new DBServer();
            configure?.Invoke(dbServer);
            
            services.AddSingleton(dbServer);
            services.AddSingleton<IMetadataProvider, MetadataProvider>();
            services.AddSingleton<ISqlGenerator, SqlGenerator>();
            
            // EntityContext her request'te yeni bir instance (Scoped) olarak oluşturulmalı
            services.AddScoped<EntityContext>();

            // Logging: Configure Serilog if not already set up
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });

            // Health Checks
            services.AddHealthChecks()
                    .AddCheck<DbConnectionHealthCheck>("OmerkckEF_Database_Check");
            
            return services;
        }
    }
}
