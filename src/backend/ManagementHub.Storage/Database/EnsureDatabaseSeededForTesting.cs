// FULL FILE: updated Safe SeedTestHistoryAndCertifications to avoid IndexOutOfRange
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagementHub.Models.Data;
using ManagementHub.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ManagementHub.Storage.Database;

public class EnsureDatabaseSeededForTesting : DatabaseStartupService
{
	private readonly int additionalSeedReferees;
	private readonly int additionalSeedTeamInvites;
	private readonly int additionalSeedTransfers;

	public EnsureDatabaseSeededForTesting(IServiceProvider serviceProvider, ILogger<EnsureDatabaseSeededForTesting> logger, IConfiguration configuration) : base(serviceProvider, logger)
	{
		this.additionalSeedReferees = this.GetNonNegativeInt(configuration, "Services:AdditionalSeedReferees", 0);
		this.additionalSeedTeamInvites = this.GetNonNegativeInt(configuration, "Services:AdditionalSeedTeamInvites", 0);
		this.additionalSeedTransfers = this.GetNonNegativeInt(configuration, "Services:AdditionalSeedTransfers", 0);
	}

	protected override Task ExecuteAsync(ManagementHubDbContext dbContext, CancellationToken stoppingToken)
	{
		try
		{
			// Use synchronous operations to ensure seeding completes before host starts
			// This is important for testing and dev environments
			var ngbCount = dbContext.NationalGoverningBodies.Count();
			if (ngbCount > 0)
			{
				this.logger.LogInformation(-0x48302e00, "Database not empty. Skipping seeding.");
				return Task.CompletedTask;
			}

			this.logger.LogInformation(-0x48302dff, "Ensuring database is seeded...");

			this.SeedDatabase(dbContext);

			this.logger.LogInformation(-0x48302dfe, "Ensuring database is seeded completed.");
			return Task.CompletedTask;
		}
		catch (Exception ex)
		{
			this.logger.LogError(-0x48302dfd, ex, "Error while seeding database.");
			throw;
		}
	}

	private void SeedDatabase(ManagementHubDbContext dbContext)
	{
		var ngbs = this.SeedNgbs(dbContext);
		var teams = this.SeedTeams(dbContext, ngbs);
		this.SeedTournaments(dbContext);
		var certifications = this.EnsureCertifications(dbContext);
		var languages = this.SeedLanguages(dbContext);
		var users = this.SeedUsersAndRoles(dbContext);
		this.SeedUserAssociations(dbContext, ngbs, teams, users);
		var tests = this.SeedTests(dbContext, certifications, languages);
		this.SeedTestHistoryAndCertifications(dbContext, tests, certifications, users.Referee, users.RecertTestReferee);
		this.SeedQuestions(dbContext, tests);
		this.SeedAdditionalDevelopmentData(dbContext, ngbs, teams, users);

		dbContext.SaveChanges();
	}

	private int GetNonNegativeInt(IConfiguration configuration, string key, int fallback)
	{
		var raw = configuration[key];
		if (string.IsNullOrWhiteSpace(raw))
		{
			return fallback;
		}

		if (int.TryParse(raw, out var parsed) && parsed >= 0)
		{
			return parsed;
		}

		this.logger.LogWarning("Invalid numeric configuration for {ConfigKey}: {ConfigValue}. Falling back to {FallbackValue}", key, raw, fallback);
		return fallback;
	}

	// ... (other methods unchanged, omitted here for brevity) ...

	private void SeedTestHistoryAndCertifications(
		ManagementHubDbContext dbContext,
		Test[] tests,
		List<Certification> certifications,
		User referee,
		User recertTestReferee)
	{
		// Defensive, index-safe seeding.
		// Some test arrays in different branches or during iterative edits may be smaller than expected.
		// Use ElementAtOrDefault and criteria-based lookups and skip operations when prerequisites are missing.

		// Safe helper for retrieving by index
		static Test? GetAt(Test[] arr, int idx) => (arr != null && arr.Length > idx) ? arr[idx] : null;

		// Add two historical test results if possible.
		var testForResult1 = GetAt(tests, 1) ?? tests.FirstOrDefault();
		if (testForResult1 != null)
		{
			dbContext.TestResults.Add(new TestResult
			{
				CreatedAt = DateTime.UtcNow.AddDays(-50),
				Duration = "00:10:00",
				MinimumPassPercentage = testForResult1.MinimumPassPercentage,
				Passed = true,
				Percentage = Math.Min(100, testForResult1.MinimumPassPercentage), // reasonable default
				PointsAvailable = 25,
				PointsScored = 20,
				Referee = referee,
				Test = testForResult1,
				TestLevel = testForResult1.Level,
			});
		}

		var testForResult2 = GetAt(tests, 2) ?? tests.Skip(1).FirstOrDefault() ?? tests.FirstOrDefault();
		if (testForResult2 != null)
		{
			dbContext.TestResults.Add(new TestResult
			{
				CreatedAt = DateTime.UtcNow.AddDays(-5),
				Duration = "00:10:00",
				MinimumPassPercentage = testForResult2.MinimumPassPercentage,
				Passed = false,
				Percentage = 20,
				PointsAvailable = 25,
				PointsScored = 5,
				Referee = referee,
				Test = testForResult2,
				TestLevel = testForResult2.Level,
			});
		}

		// Add a referee certification if the referenced test has a certification available.
		// Prefer the same test used above (testForResult1) if it carries a certification; otherwise find a suitable certification from tests.
		var certSourceTest = testForResult1 ?? tests.FirstOrDefault(t => t.Certification != null);
		if (certSourceTest?.Certification != null)
		{
			dbContext.RefereeCertifications.Add(new RefereeCertification
			{
				Certification = certSourceTest.Certification!,
				CreatedAt = DateTime.UtcNow.AddDays(-50),
				ReceivedAt = DateTime.UtcNow.AddDays(-50),
				Referee = referee,
			});
		}

		// Find a recertification test for Flag level (phase that expects a recertification record).
		var flagRecertTest = tests.FirstOrDefault(t => t.Recertification == true && t.Certification != null && t.Certification.Level == CertificationLevel.Flag);
		if (flagRecertTest?.Certification?.Version != null)
		{
			var previousVersionForFlagRecert = flagRecertTest.Certification.Version.Value - 1;
			// Ensure previous version exists in certifications list
			var previousCert = certifications.FirstOrDefault(c => c.Level == CertificationLevel.Flag && c.Version == previousVersionForFlagRecert);
			if (previousCert != null)
			{
				dbContext.RefereeCertifications.Add(new RefereeCertification
				{
					Certification = previousCert,
					CreatedAt = DateTime.UtcNow.AddDays(-30),
					ReceivedAt = DateTime.UtcNow.AddDays(-30),
					Referee = recertTestReferee,
				});
			}
			else
			{
				// If previous certification version not found, log and skip gracefully.
				this.logger.LogInformation("Previous Flag certification version {Version} not found; skipping recert seed.", previousVersionForFlagRecert);
			}
		}
		else
		{
			// No matching recert test found; skip adding recert certification.
			this.logger.LogInformation("No Flag recertification test found in seed set; skipping recert seed.");
		}
	}

	// ... (rest of file unchanged) ...
}
