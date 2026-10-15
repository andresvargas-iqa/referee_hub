using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ManagementHub.Storage;
using ManagementHub.Storage.Database;
using ManagementHub.Storage.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ManagementHub.UnitTests;

public class InMemoryDatabaseStartupTests
{
	[Fact]
	public async Task Startup_CreatesSchemaVisibleToAnotherScope()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddManagementHubStorage(inMemoryStorage: true, seedDatabase: false);
		await using var provider = services.BuildServiceProvider();
		var hostedServices = provider.GetServices<IHostedService>();
		foreach (var hostedService in hostedServices)
		{
			if (hostedService is EnsureDatabaseConnectionKeptAliveService or EnsureDatabaseCreatedService)
			{
				await hostedService.StartAsync(CancellationToken.None);
			}
		}

		await using var scope = provider.CreateAsyncScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<ManagementHubDbContext>();
		(await dbContext.NationalGoverningBodies.CountAsync()).Should().Be(0);
		foreach (var hostedService in hostedServices)
		{
			if (hostedService is EnsureDatabaseConnectionKeptAliveService or EnsureDatabaseCreatedService)
			{
				await hostedService.StopAsync(CancellationToken.None);
			}
		}
	}
}