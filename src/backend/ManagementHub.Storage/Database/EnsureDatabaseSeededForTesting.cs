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

	private void SeedAdditionalDevelopmentData(
		ManagementHubDbContext dbContext,
		IReadOnlyList<NationalGoverningBody> ngbs,
		IReadOnlyList<Team> teams,
		SeedUsers users)
	{
		if (this.additionalSeedReferees == 0 && this.additionalSeedTeamInvites == 0 && this.additionalSeedTransfers == 0)
		{
			return;
		}

		this.logger.LogInformation(
			"Seeding additional dev dataset: {RefereeCount} referees, {InviteCount} team invites, {TransferCount} transfers.",
			this.additionalSeedReferees,
			this.additionalSeedTeamInvites,
			this.additionalSeedTransfers);

		var now = DateTime.UtcNow;
		var random = new Random(421337);
		const string passwordHash = "$2a$11$YURdUdxxppPle1z32ZExtu8Jk7lXJxpcckfOtpznfw3VT2zsZmzne";

		var extraReferees = this.CreateExtraReferees(now, passwordHash);
		this.SeedExtraRefereeAssociations(dbContext, ngbs, teams, extraReferees, now);

		var inviteTargets = this.BuildInviteTargets(extraReferees, users);
		var seedData = new AdditionalSeedData(this.additionalSeedTeamInvites, this.additionalSeedTransfers);

		this.SeedAdditionalTeamInvites(dbContext, teams, users, inviteTargets, now, seedData);
		this.SeedAdditionalTransfers(teams, users, inviteTargets, now, random, seedData);

		dbContext.TeamInvitations.AddRange(seedData.TeamInvitations);
		dbContext.TeamPlayerActivities.AddRange(seedData.TeamPlayerActivities);
		dbContext.NgbTransferApprovals.AddRange(seedData.TransferApprovals);
	}

	private List<User> CreateExtraReferees(DateTime now, string passwordHash)
	{
		var extraReferees = new List<User>(this.additionalSeedReferees);
		for (var i = 1; i <= this.additionalSeedReferees; i++)
		{
			extraReferees.Add(new User
			{
				CreatedAt = now.AddMinutes(-(i + 10)),
				Email = $"dev.referee.{i:D4}@example.test",
				EncryptedPassword = passwordHash,
				FirstName = "Dev",
				LastName = $"Referee{i:D4}",
			});
		}

		return extraReferees;
	}

	private void SeedExtraRefereeAssociations(
		ManagementHubDbContext dbContext,
		IReadOnlyList<NationalGoverningBody> ngbs,
		IReadOnlyList<Team> teams,
		IReadOnlyList<User> extraReferees,
		DateTime now)
	{
		dbContext.Users.AddRange(extraReferees);
		dbContext.Roles.AddRange(extraReferees.Select(referee => new Role
		{
			AccessType = UserAccessType.Referee,
			User = referee,
			CreatedAt = now,
		}));

		dbContext.RefereeLocations.AddRange(extraReferees.Select((referee, index) => new RefereeLocation
		{
			Referee = referee,
			AssociationType = RefereeNgbAssociationType.Primary,
			NationalGoverningBody = ngbs[index % ngbs.Count],
			CreatedAt = now.AddDays(-(index % 90)),
			UpdatedAt = now,
		}));

		var playerAssociations = new List<RefereeTeam>(extraReferees.Count);
		for (var index = 0; index < extraReferees.Count; index++)
		{
			if (index % 3 == 0)
			{
				continue;
			}

			playerAssociations.Add(new RefereeTeam
			{
				Referee = extraReferees[index],
				AssociationType = RefereeTeamAssociationType.Player,
				Team = teams[index % teams.Count],
				CreatedAt = now.AddDays(-(index % 120)),
				UpdatedAt = now,
			});
		}

		dbContext.RefereeTeams.AddRange(playerAssociations);
	}

	private IReadOnlyList<User> BuildInviteTargets(IReadOnlyList<User> extraReferees, SeedUsers users)
	{
		return extraReferees.Count > 0
			? extraReferees
			: new List<User> { users.Referee, users.PlayerSarah, users.CoachMike, users.RecertTestReferee };
	}

	private void SeedAdditionalTeamInvites(
		ManagementHubDbContext dbContext,
		IReadOnlyList<Team> teams,
		SeedUsers users,
		IReadOnlyList<User> inviteTargets,
		DateTime now,
		AdditionalSeedData seedData)
	{
		for (var i = 1; i <= this.additionalSeedTeamInvites; i++)
		{
			var targetUser = inviteTargets[(i - 1) % inviteTargets.Count];
			var destinationTeam = teams[(i - 1) % teams.Count];
			var createdAt = now.AddHours(-i);

			var invitation = new TeamInvitation
			{
				Team = destinationTeam,
				Email = targetUser.Email,
				Initiator = users.TeamManager,
				CreatedAt = createdAt,
			};

			var resultActivityType = this.ApplyInviteOutcomeAndMembership(dbContext, invitation, targetUser, destinationTeam, createdAt, i);
			seedData.TeamInvitations.Add(invitation);
			seedData.TeamPlayerActivities.Add(BuildInviteCreatedActivity(destinationTeam, targetUser, users.TeamManager, createdAt));

			if (resultActivityType != TeamPlayerActivityType.InviteCreated)
			{
				seedData.TeamPlayerActivities.Add(new TeamPlayerActivity
				{
					Team = destinationTeam,
					User = targetUser,
					Email = targetUser.Email,
					Initiator = users.TeamManager,
					ActivityType = resultActivityType,
					CreatedAt = createdAt.AddMinutes(45),
				});
			}
		}
	}

	private TeamPlayerActivityType ApplyInviteOutcomeAndMembership(
		ManagementHubDbContext dbContext,
		TeamInvitation invitation,
		User targetUser,
		Team destinationTeam,
		DateTime createdAt,
		int index)
	{
		if (index % 6 == 0)
		{
			invitation.RevokedAt = createdAt.AddMinutes(30);
			return TeamPlayerActivityType.InviteRevoked;
		}

		if (index % 5 == 0)
		{
			invitation.DeclinedAt = createdAt.AddMinutes(40);
			invitation.RespondedByUser = targetUser;
			return TeamPlayerActivityType.InviteDeclined;
		}

		if (index % 4 != 0)
		{
			return TeamPlayerActivityType.InviteCreated;
		}

		invitation.AcceptedAt = createdAt.AddMinutes(20);
		invitation.RespondedByUser = targetUser;

		var acceptedAt = invitation.AcceptedAt.Value;

		// Check for existing player membership first in the change tracker, then the database.
		RefereeTeam? existingMembership = dbContext.ChangeTracker.Entries<RefereeTeam>()
			.Select(e => e.Entity)
			.FirstOrDefault(rt =>
				((rt.Referee != null && rt.Referee == targetUser) || (rt.RefereeId.HasValue && rt.RefereeId.Value == targetUser.Id))
				&& rt.AssociationType == RefereeTeamAssociationType.Player);

		if (existingMembership == null)
		{
			existingMembership = dbContext.RefereeTeams
				.FirstOrDefault(rt => rt.RefereeId == targetUser.Id && rt.AssociationType == RefereeTeamAssociationType.Player);
		}

		if (existingMembership != null)
		{
			// Update existing membership to the new team instead of inserting a duplicate.
			existingMembership.Team = destinationTeam;
			existingMembership.UpdatedAt = acceptedAt;
		}
		else
		{
			dbContext.RefereeTeams.Add(new RefereeTeam
			{
				Referee = targetUser,
				AssociationType = RefereeTeamAssociationType.Player,
				Team = destinationTeam,
				CreatedAt = acceptedAt,
				UpdatedAt = acceptedAt,
			});
		}

		return TeamPlayerActivityType.InviteAccepted;
	}

	private void SeedAdditionalTransfers(
		IReadOnlyList<Team> teams,
		SeedUsers users,
		IReadOnlyList<User> inviteTargets,
		DateTime now,
		Random random,
		AdditionalSeedData seedData)
	{
		var eligibleTransferTeams = teams
			.Where(team =>
				team.GroupAffiliation != TeamGroupAffiliation.National
				&& team.GroupAffiliation != TeamGroupAffiliation.NotApplicable)
			.ToArray();

		if (this.additionalSeedTransfers > 0 && eligibleTransferTeams.Length < 2)
		{
			this.logger.LogWarning(
				"Skipping additional transfer seed data because fewer than 2 eligible playing teams are available (found {EligibleTeamCount}).",
				eligibleTransferTeams.Length);
		}

		var transferSeedCount = eligibleTransferTeams.Length >= 2 ? this.additionalSeedTransfers : 0;
		for (var i = 1; i <= transferSeedCount; i++)
		{
			var originTeam = eligibleTransferTeams[(i + 1) % eligibleTransferTeams.Length];
			var destinationTeam = eligibleTransferTeams[(i + 2) % eligibleTransferTeams.Length];
			if (ReferenceEquals(originTeam, destinationTeam))
			{
				destinationTeam = eligibleTransferTeams[(i + 3) % eligibleTransferTeams.Length];
			}

			var targetUser = inviteTargets[(i * 7) % inviteTargets.Count];
			var createdAt = now.AddHours(-(this.additionalSeedTeamInvites + i));
			var originNgb = originTeam.NationalGoverningBody;
			var destinationNgb = destinationTeam.NationalGoverningBody;
			if (originNgb == null || destinationNgb == null)
			{
				continue;
			}

			var transferInvite = new TeamInvitation
			{
				Team = destinationTeam,
				Email = targetUser.Email,
				Initiator = users.TeamManager,
				CreatedAt = createdAt,
				OriginTeam = originTeam,
				IsInternalTransfer = originNgb.Id == destinationNgb.Id,
			};

			if (i % 9 == 0)
			{
				transferInvite.RevokedAt = createdAt.AddMinutes(50);
			}

			seedData.TeamInvitations.Add(transferInvite);
			seedData.TeamPlayerActivities.Add(BuildInviteCreatedActivity(destinationTeam, targetUser, users.TeamManager, createdAt));

			if (transferInvite.RevokedAt != null)
			{
				seedData.TeamPlayerActivities.Add(new TeamPlayerActivity
				{
					Team = destinationTeam,
					User = targetUser,
					Email = targetUser.Email,
					Initiator = users.TeamManager,
					ActivityType = TeamPlayerActivityType.InviteRevoked,
					CreatedAt = transferInvite.RevokedAt.Value,
				});
			}

			this.AddTransferApprovalsForInvite(seedData.TransferApprovals, transferInvite, originNgb, destinationNgb, createdAt, random, users, i);
		}
	}

	private static TeamPlayerActivity BuildInviteCreatedActivity(Team destinationTeam, User targetUser, User initiator, DateTime createdAt)
	{
		return new TeamPlayerActivity
		{
			Team = destinationTeam,
			User = targetUser,
			Email = targetUser.Email,
			Initiator = initiator,
			ActivityType = TeamPlayerActivityType.InviteCreated,
			CreatedAt = createdAt,
		};
	}

	private void AddTransferApprovalsForInvite(
		ICollection<NgbTransferApproval> transferApprovals,
		TeamInvitation transferInvite,
		NationalGoverningBody originNgb,
		NationalGoverningBody destinationNgb,
		DateTime createdAt,
		Random random,
		SeedUsers users,
		int index)
	{
		if (originNgb.Id == destinationNgb.Id)
		{
			transferApprovals.Add(new NgbTransferApproval
			{
				TeamInvitation = transferInvite,
				Ngb = originNgb,
				IsOriginNgb = true,
				CreatedAt = createdAt,
				ApprovedAt = index % 3 == 0 ? createdAt.AddMinutes(15) : null,
				ReviewedByUser = index % 3 == 0 ? users.NgbAdmin : null,
			});
			return;
		}

		var originApproval = new NgbTransferApproval
		{
			TeamInvitation = transferInvite,
			Ngb = originNgb,
			IsOriginNgb = true,
			CreatedAt = createdAt,
		};

		var destinationApproval = new NgbTransferApproval
		{
			TeamInvitation = transferInvite,
			Ngb = destinationNgb,
			IsOriginNgb = false,
			CreatedAt = createdAt,
		};

		if (index % 7 == 0)
		{
			originApproval.RejectedAt = createdAt.AddMinutes(20 + random.Next(10));
			originApproval.ReviewedByUser = users.NgbAdmin;
		}
		else
		{
			originApproval.ApprovedAt = createdAt.AddMinutes(10 + random.Next(20));
			originApproval.ReviewedByUser = users.NgbAdmin;

			if (index % 2 == 0)
			{
				destinationApproval.ApprovedAt = createdAt.AddMinutes(40 + random.Next(20));
				destinationApproval.ReviewedByUser = users.NgbAdmin;
			}
		}

		transferApprovals.Add(originApproval);
		transferApprovals.Add(destinationApproval);
	}

	private sealed class AdditionalSeedData
	{
		public AdditionalSeedData(int additionalSeedTeamInvites, int additionalSeedTransfers)
		{
			this.TeamInvitations = new List<TeamInvitation>(additionalSeedTeamInvites + additionalSeedTransfers);
			this.TeamPlayerActivities = new List<TeamPlayerActivity>(additionalSeedTeamInvites + additionalSeedTransfers);
			this.TransferApprovals = new List<NgbTransferApproval>(additionalSeedTransfers * 2);
		}

		public List<TeamInvitation> TeamInvitations { get; }
		public List<TeamPlayerActivity> TeamPlayerActivities { get; }
		public List<NgbTransferApproval> TransferApprovals { get; }
	}

	private NationalGoverningBody[] SeedNgbs(ManagementHubDbContext dbContext)
	{
		var ngbs = new[]
		{
			new NationalGoverningBody
			{
				CountryCode = "ARG",
				Name = "Asociación de Quidditch Argentina",
				Country = "Argentina",
				Region = NgbRegion.SouthAmerica,
				MembershipStatus = NgbMembershipStatus.Full,
				PlayerCount = 75,
				Website = "https://www.facebook.com/asociaciondequidditch.arg/",
				CreatedAt = DateTime.UtcNow,
			},
			new NationalGoverningBody
			{
				CountryCode = "AUS",
				Name = "Quidditch Australia",
				Country = "Australia",
				Region = NgbRegion.Asia,
				MembershipStatus = NgbMembershipStatus.Full,
				PlayerCount = 700,
				Website = "https://www.quidditch.info/",
				CreatedAt = DateTime.UtcNow,
			},
			new NationalGoverningBody
			{
				CountryCode = "BRA",
				Name = "Associação Brasileira de Quadball",
				Country = "Brazil",
				Region = NgbRegion.SouthAmerica,
				MembershipStatus = NgbMembershipStatus.Developing,
				PlayerCount = 319,
				Website = "https://abrquadribol.wordpress.com/",
				CreatedAt = DateTime.UtcNow,
			},
			new NationalGoverningBody
			{
				CountryCode = "POL",
				Name = "Polska Liga Quidditcha",
				Country = "Poland",
				Region = NgbRegion.Europe,
				MembershipStatus = NgbMembershipStatus.Full,
				PlayerCount = 110,
				Website = "https://polskaligaquidditcha.pl/",
				CreatedAt = DateTime.UtcNow,
			},
			new NationalGoverningBody
			{
				CountryCode = "USA",
				Name = "US Quadball",
				Country = "United States",
				Region = NgbRegion.NorthAmerica,
				MembershipStatus = NgbMembershipStatus.Full,
				PlayerCount = 1681,
				Website = "https://www.usquadball.org/",
				CreatedAt = DateTime.UtcNow,
			},
			new NationalGoverningBody
			{
				CountryCode = "DEU",
				Name = "QBund",
				Country = "Germany",
				Region = NgbRegion.Europe,
				MembershipStatus = NgbMembershipStatus.Full,
				PlayerCount = 600,
				Website = "https://www.usquadball.org/",
				CreatedAt = DateTime.UtcNow,
			},
		};

		dbContext.NationalGoverningBodies.AddRange(ngbs);
		return ngbs;
	}

	private List<Team> SeedTeams(ManagementHubDbContext dbContext, NationalGoverningBody[] ngbs)
	{
		var teams = new List<Team>
		{
			new Team
			{
				City = "New York",
				Country = "USA",
				Name = "Yankees",
				NationalGoverningBody = ngbs.Single(n => n.CountryCode == "USA"),
				GroupAffiliation = TeamGroupAffiliation.Community,
				CreatedAt = DateTime.UtcNow,
				JoinedAt = DateTime.UtcNow,
				Status = TeamStatus.Competitive,
				UpdatedAt = DateTime.UtcNow,
				Description = "New York's premier community quidditch team",
				ContactEmail = "contact@yankees-quidditch.example.com",
			},
			new Team
			{
				City = "Los Angeles",
				Country = "USA",
				Name = "LA Bisons",
				NationalGoverningBody = ngbs.Single(n => n.CountryCode == "USA"),
				GroupAffiliation = TeamGroupAffiliation.University,
				CreatedAt = DateTime.UtcNow,
				JoinedAt = DateTime.UtcNow,
				Status = TeamStatus.Competitive,
				UpdatedAt = DateTime.UtcNow,
				Description = "University of Los Angeles competitive quidditch team",
				ContactEmail = "labisons@university.example.edu",
			},
