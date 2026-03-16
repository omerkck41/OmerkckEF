using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OmerkckEF.Biscom;
using OmerkckEF.Biscom.DBContext;
using OmerkckEF.Biscom.Interfaces;
using Xunit;

namespace OmerkckEF.Bisco.Tests
{
    public class DITests
    {
        [Fact]
        public void AddOmerkckEF_ShouldRegisterRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddOmerkckEF(config =>
            {
                config.DbServerId = 1;
                config.DbSchema = "TestDb";
            });

            var serviceProvider = services.BuildServiceProvider();

            // Assert
            Assert.NotNull(serviceProvider.GetService<IMetadataProvider>());
            Assert.NotNull(serviceProvider.GetService<ISqlGenerator>());
            Assert.NotNull(serviceProvider.GetService<EntityContext>());
            Assert.NotNull(serviceProvider.GetService<DBServer>());
            
            // New Services from Tech Audit
            Assert.NotNull(serviceProvider.GetService<ILoggerFactory>());
            Assert.NotNull(serviceProvider.GetService<HealthCheckService>());
            
            var dbServer = serviceProvider.GetService<DBServer>();
            Assert.Equal("TestDb", dbServer!.DbSchema);
        }
    }
}
