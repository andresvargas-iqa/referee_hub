using Amazon.S3.Model;
using ManagementHub.Models.Abstraction.Commands;
using ManagementHub.Models.Abstraction.Contexts.Providers;
using ManagementHub.Models.Domain.General;
using ManagementHub.Models.Domain.Ngb;
using ManagementHub.Models.Domain.Notification;
using ManagementHub.Models.Domain.Team;
using ManagementHub.Models.Domain.User;
using ManagementHub.Models.Domain.User.Roles;
using ManagementHub.Models.Enums;
using ManagementHub.Models.Exceptions;
using ManagementHub.Service.Areas.Tournaments;
using ManagementHub.Service.Authorization;
using ManagementHub.Service.Contexts;
using ManagementHub.Service.Filtering;
using ManagementHub.Service.Services;
using ManagementHub.Storage;
using ManagementHub.Storage.Collections;
using ManagementHub.Storage.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManagementHub.Service.Areas.Ngbs;

/// <summary>
/// Actions related to NGBs.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v2/[controller]")]
[Produces("application/json")]
public class NgbsController : ControllerBase
{
	private readonly IUserContextAccessor contextAccessor;
	private readonly INgbContextProvider ngbContextProvider;
	private readonly ITeamContextProvider teamContextProvider;
	private readonly ITournamentContextProvider tournamentContextProvider;
	private readonly ISocialAccountsProvider socialAccountsProvider;
	private readonly IUpdateUserAvatarCommand updateUserAvatarCommand;
	private readonly IUpdateNgbAdminRoleCommand updateNgbAdminRoleCommand;
	private readonly IUpdateTeamManagerRoleCommand updateTeamManagerRoleCommand;
	private readonly IUpdateUserDataCommand updateUserDataCommand;
	private readonly INotificationService notificationService;
	private readonly ManagementHubDbContext dbContext;

	public NgbsController(
		IUserContextAccessor contextAccessor,
		INgbContextProvider ngbContextProvider,
		ITeamContextProvider teamContextProvider,
		ITournamentContextProvider tournamentContextProvider,
		ISocialAccountsProvider socialAccountsProvider,
		IUpdateUserAvatarCommand updateUserAvatarCommand,
		IUpdateNgbAdminRoleCommand updateNgbAdminRoleCommand,
		IUpdateTeamManagerRoleCommand updateTeamManagerRoleCommand,
		IUpdateUserDataCommand updateUserDataCommand,
		INotificationService notificationService,
		ManagementHubDbContext dbContext)
	{
		this.contextAccessor = contextAccessor;
		this.ngbContextProvider = ngbContextProvider;
		this.teamContextProvider = teamContextProvider;
		this.tournamentContextProvider = tournamentContextProvider;
		this.socialAccountsProvider = socialAccountsProvider;
		this.updateUserAvatarCommand = updateUserAvatarCommand;
		this.updateNgbAdminRoleCommand = updateNgbAdminRoleCommand;
		this.updateTeamManagerRoleCommand = updateTeamManagerRoleCommand;
		this.updateUserDataCommand = updateUserDataCommand;
		this.notificationService = notificationService;
		this.dbContext = dbContext;
	}

	/// <summary>
	/// List NGBs registered in the Hub.
	/// </summary>
	[HttpGet]
	[Tags("Ngb")]
	public Filtered<NgbViewModel> GetNgbs([FromQuery] FilteringParameters filtering)
	{
		return this.ngbContextProvider.QueryNgbs().Select(ngb => new NgbViewModel
		{
			CountryCode = ngb.NgbId.NgbCode,
			Name = ngb.NgbData.Name,
			Acronym = ngb.NgbData.Acronym,
			Country = ngb.NgbData.Country,
			MembershipStatus = ngb.NgbData.MembershipStatus,
			PlayerCount = ngb.NgbData.PlayerCount,
			Region = ngb.NgbData.Region,
			Website = ngb.NgbData.Website,
		}).AsFiltered();
	}

	/// <summary>
	/// Get NGB country codes that have at least one team with the specified group affiliations.
	/// Used to filter the NGB selector to only show regions with eligible teams for a tournament type.
	/// </summary>
	[HttpGet("with-eligible-teams")]
	[Tags("Ngb")]
	public ActionResult<IEnumerable<NgbIdentifier>> GetNgbsWithEligibleTeams([FromQuery] TeamGroupAffiliation[] groupAffiliations)
	{
		if (groupAffiliations == null || groupAffiliations.Length == 0)
		{
			return this.Ok(Array.Empty<NgbIdentifier>());
		}

		var codes = this.teamContextProvider.GetNgbCodesWithTeams(groupAffiliations);
		return this.Ok(codes);
	}

	/// <summary>
	/// Get NGB profile information.
	/// </summary>
	[HttpGet("{ngb}")]
	[Tags("Ngb")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	public async Task<NgbInfoViewModel> GetNgbInfo([FromRoute] NgbIdentifier ngb)
	{
		var context = await this.ngbContextProvider.GetNgbContextAsync(ngb);

		var socialAccounts = await this.socialAccountsProvider.QueryNgbSocialAccounts(NgbConstraint.Single(ngb));
		var stats = await this.ngbContextProvider.GetCurrentNgbStatsAsync(ngb);
		var historicalStats = await this.ngbContextProvider.GetHistoricalNgbStatsAsync(ngb);
		var avatarUri = await this.ngbContextProvider.GetNgbAvatarUriAsync(ngb);
		var adminEmails = await this.ngbContextProvider.GetNgbAdminEmails(ngb);

		return new NgbInfoViewModel
		{
			Acronym = context.NgbData.Acronym,
			Country = context.NgbData.Country,
			CountryCode = context.NgbId.NgbCode,
			MembershipStatus = context.NgbData.MembershipStatus,
			Name = context.NgbData.Name,
			PlayerCount = context.NgbData.PlayerCount,
			Region = context.NgbData.Region,
			SocialAccounts = socialAccounts.GetValueOrDefault(ngb, Enumerable.Empty<SocialAccount>()),
			CurrentStats = stats,
			HistoricalStats = historicalStats,
			Website = context.NgbData.Website,
			AvatarUri = avatarUri,
			AdminEmails = adminEmails,
		};
	}

	[HttpPut("{ngb}")]
	[Tags("Ngb")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	public async Task UpdateNgb([FromRoute] NgbIdentifier ngb, [FromBody] NgbUpdateModel model)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();
		var permissionConstraint = userContext.Roles.OfType<NgbAdminRole>().FirstOrDefault()?.Ngb ?? NgbConstraint.Empty();

		if (!permissionConstraint.AppliesTo(ngb))
		{
			throw new AccessDeniedException(ngb.ToString());
		}

		var context = await this.ngbContextProvider.GetNgbContextAsync(ngb);

		var ngbData = new NgbData
		{
			Name = model.Name,
			Country = model.Country,
			Acronym = model.Acronym,
			Website = model.Website,
			PlayerCount = model.PlayerCount,
			Region = context.NgbData.Region,
			MembershipStatus = context.NgbData.MembershipStatus,
		};

		await this.ngbContextProvider.UpdateNgbInfoAsync(ngb, ngbData);

		_ = await this.socialAccountsProvider.UpdateNgbSocialAccounts(ngb, model.SocialAccounts);
	}

	[HttpPut("{ngb}/avatar")]
	[Tags("Ngb")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	public async Task<Uri> UpdateNgbAvatar([FromRoute] NgbIdentifier ngb, IFormFile avatarBlob)
	{
		var avatarUri = await this.updateUserAvatarCommand.UpdateNgbAvatarAsync(
			ngb,
			avatarBlob.ContentType,
			avatarBlob.OpenReadStream(),
			this.HttpContext.RequestAborted);
		return avatarUri;
	}

	[HttpPost("{ngb}/admins")]
	[Tags("Ngb")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NgbAdminCreationStatus))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(NgbAdminCreationStatus))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NgbAdminCreationStatus))]
	public async Task<NgbAdminCreationStatus> AddNgbAdmin([FromRoute] NgbIdentifier ngb, [FromBody] NgbAdminCreationModel adminModel)
	{
		if (!Email.TryParse(adminModel.Email, out var email))
		{
			this.Response.StatusCode = StatusCodes.Status400BadRequest;
			return NgbAdminCreationStatus.InvalidEmail;
		}

		var result = await this.updateNgbAdminRoleCommand.AddNgbAdminRoleAsync(ngb, email, adminModel.CreateAccountIfNotExists);
		switch (result.Result)
		{
			case IUpdateNgbAdminRoleCommand.AddRoleResult.UserDoesNotExist:
				this.Response.StatusCode = StatusCodes.Status404NotFound;
				return NgbAdminCreationStatus.UserDoesNotExist;
			case IUpdateNgbAdminRoleCommand.AddRoleResult.RoleAdded:
			{
				if (result.UserId.HasValue)
				{
					await this.notificationService.CreateNgbAdminAssignmentNotificationAsync(
						result.UserId.Value,
						ngb,
						this.HttpContext.RequestAborted);
				}
				return NgbAdminCreationStatus.AdminRoleAdded;
			}
			case IUpdateNgbAdminRoleCommand.AddRoleResult.UserCreatedWithRole:
			{
				if (result.UserId.HasValue)
				{
					await this.notificationService.CreateNgbAdminAssignmentNotificationAsync(
						result.UserId.Value,
						ngb,
						this.HttpContext.RequestAborted);
				}
				return NgbAdminCreationStatus.AdminUserCreated;
			}
			default: throw new InvalidOperationException($"Unexpected result {result.Result}");
		}
	}

	[HttpDelete("{ngb}/admins")]
	[Tags("Ngb")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
	public async Task DeleteNgbAdmin([FromRoute] NgbIdentifier ngb, [FromQuery] string email)
	{
		if (!Email.TryParse(email, out var email_))
		{
			this.Response.StatusCode = StatusCodes.Status400BadRequest;
			return;
		}

		var result = await this.updateNgbAdminRoleCommand.DeleteNgbAdminRoleAsync(ngb, email_);
		this.Response.StatusCode = result ? StatusCodes.Status200OK : StatusCodes.Status404NotFound;
		return;
	}

	[HttpPut("api/v2/admin/[controller]/{ngb}")]
	[Tags("Ngb")]
	[Authorize(AuthorizationPolicies.IqaAdminPolicy)]
	public async Task AdminUpdateNgb([FromRoute] NgbIdentifier ngb, [FromBody] AdminNgbUpdateModel model)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();

		var context = await this.ngbContextProvider.GetNgbContextAsync(ngb);

		var ngbData = new NgbData
		{
			Name = model.Name,
			Country = model.Country,
			Acronym = model.Acronym,
			Website = model.Website,
			PlayerCount = model.PlayerCount,
			Region = model.Region,
			MembershipStatus = model.MembershipStatus,
		};

		await this.ngbContextProvider.UpdateNgbInfoAsync(ngb, ngbData);

		_ = await this.socialAccountsProvider.UpdateNgbSocialAccounts(ngb, model.SocialAccounts);
	}

	[HttpPost("api/v2/admin/[controller]/{ngb}")]
	[Tags("Ngb")]
	[Authorize(AuthorizationPolicies.IqaAdminPolicy)]
	public async Task AdminCreateNgb([FromRoute] NgbIdentifier ngb, [FromBody] AdminNgbUpdateModel model)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();

		try
		{
			_ = await this.ngbContextProvider.GetNgbContextAsync(ngb);
			throw new InvalidOperationException($"NGB {ngb} already exists!");
		}
		catch (NotFoundException)
		{
			// expected
		}

		var ngbData = new NgbData
		{
			Name = model.Name,
			Country = model.Country,
			Acronym = model.Acronym,
			Website = model.Website,
			PlayerCount = model.PlayerCount,
			Region = model.Region,
			MembershipStatus = model.MembershipStatus,
		};

		await this.ngbContextProvider.CreateNgbAsync(ngb, ngbData);
	}

	/// <summary>
	/// List the teams registered under the NGB.
	/// </summary>
	[HttpGet("{ngb}/teams")]
	[Tags("Team")]
	public async Task<Filtered<NgbTeamViewModel>> GetNgbTeams([FromRoute] NgbIdentifier ngb, [FromQuery] FilteringParameters filtering)
	{
		var socialAccounts = await this.socialAccountsProvider.QueryTeamSocialAccounts(NgbConstraint.Single(ngb));
		var emptySocialAccounts = Enumerable.Empty<SocialAccount>();
		var teams = await this.teamContextProvider.GetTeams(NgbConstraint.Single(ngb)).ToListAsync();
		var logoUris = new Dictionary<TeamIdentifier, Uri?>();
		foreach (var team in teams)
		{
			logoUris[team.TeamId] = await this.teamContextProvider.GetTeamLogoUriAsync(team.TeamId, this.HttpContext.RequestAborted);
		}

		return teams.Select(team => new NgbTeamViewModel
		{
			TeamId = team.TeamId,
			City = team.TeamData.City,
			GroupAffiliation = team.TeamData.GroupAffiliation,
			Name = team.TeamData.Name,
			Status = team.TeamData.Status,
			State = team.TeamData.State,
			Country = team.TeamData.Country,
			JoinedAt = DateOnly.FromDateTime(team.TeamData.JoinedAt),
			SocialAccounts = socialAccounts.GetValueOrDefault(team.TeamId, emptySocialAccounts),
			LogoUri = logoUris.GetValueOrDefault(team.TeamId),
			Description = team.TeamData.Description,
			ContactEmail = team.TeamData.ContactEmail,
		}).AsFiltered();
	}

	[HttpPost("{ngb}/teams")]
	[Tags("Team")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	public async Task<NgbTeamViewModel> CreateNgbTeam([FromRoute] NgbIdentifier ngb, [FromBody] NgbTeamViewModel viewModel)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();
		var permissionConstraint = userContext.Roles.OfType<NgbAdminRole>().FirstOrDefault()?.Ngb ?? NgbConstraint.Empty();

		if (!permissionConstraint.AppliesTo(ngb))
		{
			throw new AccessDeniedException(ngb.ToString());
		}

		if (viewModel.TeamId != default)
		{
			throw new ArgumentException("TeamId must not be specified when creating a team. Did you mean to use PUT method to update a team?");
		}

		var teamData = new TeamData
		{
			Name = viewModel.Name,
			City = viewModel.City,
			State = viewModel.State,
			Country = viewModel.Country,
			Status = viewModel.Status,
			GroupAffiliation = viewModel.GroupAffiliation,
			JoinedAt = viewModel.JoinedAt.ToDateTime(default, DateTimeKind.Utc),
			Description = viewModel.Description,
			ContactEmail = viewModel.ContactEmail,
		};
		var team = await this.teamContextProvider.CreateTeamAsync(ngb, teamData);
		var socialAccounts = await this.socialAccountsProvider.UpdateTeamSocialAccounts(team.TeamId, viewModel.SocialAccounts);
		return new NgbTeamViewModel
		{
			TeamId = team.TeamId,
			City = team.TeamData.City,
			GroupAffiliation = team.TeamData.GroupAffiliation,
			Name = team.TeamData.Name,
			Status = team.TeamData.Status,
			State = team.TeamData.State,
			Country = team.TeamData.Country,
			JoinedAt = DateOnly.FromDateTime(team.TeamData.JoinedAt),
			SocialAccounts = socialAccounts,
			Description = team.TeamData.Description,
			ContactEmail = team.TeamData.ContactEmail,
		};
	}

	[HttpPut("{ngb}/teams/{teamId}")]
	[Tags("Team")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	public async Task<NgbTeamViewModel> UpdateNgbTeam([FromRoute] NgbIdentifier ngb, [FromRoute] TeamIdentifier teamId, [FromBody] NgbTeamViewModel viewModel)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();
		var permissionConstraint = userContext.Roles.OfType<NgbAdminRole>().FirstOrDefault()?.Ngb ?? NgbConstraint.Empty();

		if (!permissionConstraint.AppliesTo(ngb))
		{
			throw new AccessDeniedException(ngb.ToString());
		}

		if (viewModel.TeamId != teamId)
		{
			throw new ArgumentException("Team id mismatch between URL and request body.");
		}

		var teamData = new TeamData
		{
			Name = viewModel.Name,
			City = viewModel.City,
			State = viewModel.State,
			Country = viewModel.Country,
			Status = viewModel.Status,
			GroupAffiliation = viewModel.GroupAffiliation,
			JoinedAt = viewModel.JoinedAt.ToDateTime(default, DateTimeKind.Utc),
			Description = viewModel.Description,
			ContactEmail = viewModel.ContactEmail,
		};
		var team = await this.teamContextProvider.UpdateTeamAsync(ngb, teamId, teamData);
		var socialAccounts = await this.socialAccountsProvider.UpdateTeamSocialAccounts(team.TeamId, viewModel.SocialAccounts);
		return new NgbTeamViewModel
		{
			TeamId = team.TeamId,
			City = team.TeamData.City,
			GroupAffiliation = team.TeamData.GroupAffiliation,
			Name = team.TeamData.Name,
			Status = team.TeamData.Status,
			State = team.TeamData.State,
			Country = team.TeamData.Country,
			JoinedAt = DateOnly.FromDateTime(team.TeamData.JoinedAt),
			SocialAccounts = socialAccounts,
			Description = team.TeamData.Description,
			ContactEmail = team.TeamData.ContactEmail,
		};
	}

	[HttpDelete("{ngb}/teams/{teamId}")]
	[Tags("Team")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> DeleteNgbTeam([FromRoute] NgbIdentifier ngb, [FromRoute] TeamIdentifier teamId)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();
		var permissionConstraint = userContext.Roles.OfType<NgbAdminRole>().FirstOrDefault()?.Ngb ?? NgbConstraint.Empty();

		if (!permissionConstraint.AppliesTo(ngb))
		{
			throw new AccessDeniedException(ngb.ToString());
		}

		// we have to first get the team to validate it belongs to the NGB
		if (!await this.teamContextProvider.CheckTeamExistsInNgbAsync(ngb, teamId))
		{
			throw new AccessDeniedException(teamId.ToString());
		}

		_ = await this.socialAccountsProvider.UpdateTeamSocialAccounts(teamId, []);
		await this.teamContextProvider.DeleteTeamAsync(ngb, teamId);
		return this.NoContent();
	}

	/// <summary>
	/// Add a team manager to a team.
	/// NGB Admins can manage any team in their jurisdiction.
	/// Team Managers can only add managers to their own teams.
	/// </summary>
	[HttpPost("{ngb}/teams/{teamId}/managers")]
	[Tags("Team")]
	[Authorize(AuthorizationPolicies.TeamManagerOrNgbAdminPolicy)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TeamManagerCreationStatus))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(object))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(object))]
	public async Task<TeamManagerCreationStatus> AddTeamManager(
		[FromRoute] NgbIdentifier ngb,
		[FromRoute] TeamIdentifier teamId,
		[FromBody] TeamManagerCreationModel managerModel)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();

		// Verify team exists and belongs to the specified NGB
		var team = await this.teamContextProvider.GetTeamAsync(teamId, NgbConstraint.Single(ngb));
		if (team == null)
		{
			throw new NotFoundException($"Team {teamId} not found");
		}

		// Verify user has permission (either NGB admin for this NGB, or team manager for this team)
		var hasNgbAdminPermission = userContext.Roles
			.OfType<NgbAdminRole>()
			.Any(role => role.Ngb.AppliesTo(team.NgbId));

		var hasTeamManagerPermission = userContext.Roles
			.OfType<TeamManagerRole>()
			.Any(role => role.Team.AppliesTo(teamId));

		if (!hasNgbAdminPermission && !hasTeamManagerPermission)
		{
			throw new AccessDeniedException($"No permission for team {teamId}");
		}

		// Parse and validate email
		if (!Email.TryParse(managerModel.Email, out var email))
		{
			this.Response.StatusCode = StatusCodes.Status400BadRequest;
			return TeamManagerCreationStatus.InvalidEmail;
		}

		// Add manager
		var result = await this.updateTeamManagerRoleCommand.AddTeamManagerRoleAsync(
			teamId, email, managerModel.CreateAccountIfNotExists, userContext.UserId);

		if (result is IUpdateTeamManagerRoleCommand.AddRoleResult.RoleAdded or IUpdateTeamManagerRoleCommand.AddRoleResult.UserCreatedWithRole)
		{
			var userId = await this.GetUserIdentifierByEmailAsync(email, this.HttpContext.RequestAborted);
			if (userId.HasValue)
			{
				await this.notificationService.CreateTeamManagerAssignmentNotificationAsync(
					userId.Value,
					teamId,
					cancellationToken: this.HttpContext.RequestAborted);
			}
		}

		return result switch
		{
			IUpdateTeamManagerRoleCommand.AddRoleResult.UserDoesNotExist =>
				TeamManagerCreationStatus.UserDoesNotExist,
			IUpdateTeamManagerRoleCommand.AddRoleResult.RoleAdded =>
				TeamManagerCreationStatus.ManagerRoleAdded,
			IUpdateTeamManagerRoleCommand.AddRoleResult.UserCreatedWithRole =>
				TeamManagerCreationStatus.ManagerUserCreated,
			_ => throw new InvalidOperationException($"Unexpected result {result}")
		};
	}

	private async Task<UserIdentifier?> GetUserIdentifierByEmailAsync(Email email, CancellationToken cancellationToken)
	{
		var user = await this.dbContext.Users
			.WithEmail(email)
			.Select(u => new { u.Id, u.UniqueId })
			.FirstOrDefaultAsync(cancellationToken);

		if (user == null)
			return null;

		return user.UniqueId != null ? UserIdentifier.Parse(user.UniqueId) : UserIdentifier.FromLegacyUserId(user.Id);
	}

	/// <summary>
	/// Remove a team manager from a team.
	/// NGB Admins can manage any team in their jurisdiction.
	/// Team Managers can only remove managers from their own teams.
	/// </summary>
	[HttpDelete("{ngb}/teams/{teamId}/managers")]
	[Tags("Team")]
	[Authorize(AuthorizationPolicies.TeamManagerOrNgbAdminPolicy)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task DeleteTeamManager(
		[FromRoute] NgbIdentifier ngb,
		[FromRoute] TeamIdentifier teamId,
		[FromQuery] string email)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();

		// Verify team exists and belongs to the specified NGB
		var team = await this.teamContextProvider.GetTeamAsync(teamId, NgbConstraint.Single(ngb));
		if (team == null)
		{
			throw new NotFoundException($"Team {teamId} not found");
		}

		// Verify user has permission (either NGB admin for this NGB, or team manager for this team)
		var hasNgbAdminPermission = userContext.Roles
			.OfType<NgbAdminRole>()
			.Any(role => role.Ngb.AppliesTo(team.NgbId));

		var hasTeamManagerPermission = userContext.Roles
			.OfType<TeamManagerRole>()
			.Any(role => role.Team.AppliesTo(teamId));

		if (!hasNgbAdminPermission && !hasTeamManagerPermission)
		{
			throw new AccessDeniedException($"No permission for team {teamId}");
		}

		// Parse and validate email
		if (!Email.TryParse(email, out var email_))
		{
			this.Response.StatusCode = StatusCodes.Status400BadRequest;
			return;
		}

		// Remove manager
		var result = await this.updateTeamManagerRoleCommand.DeleteTeamManagerRoleAsync(
			teamId, email_);

		this.Response.StatusCode = result
			? StatusCodes.Status200OK
			: StatusCodes.Status404NotFound;
	}

	/// <summary>
	/// List team managers for a team (NGB Admin or Team Manager).
	/// </summary>
	[HttpGet("{ngb}/teams/{teamId}/managers")]
	[Tags("Team")]
	[Authorize(AuthorizationPolicies.TeamManagerOrNgbAdminPolicy)]
	public async Task<IEnumerable<TeamManagerViewModel>> GetTeamManagers(
		[FromRoute] NgbIdentifier ngb,
		[FromRoute] TeamIdentifier teamId)
	{
		// Get managers with NGB constraint - will return empty if team doesn't belong to NGB
		var managers = await this.teamContextProvider.GetTeamManagersAsync(teamId, NgbConstraint.Single(ngb));

		return managers.Select(m => new TeamManagerViewModel
		{
			Id = m.UserId,
			Name = m.Name,
			Email = m.Email
		});
	}

	/// <summary>
	/// List members (referees) associated with a team.
	/// </summary>
	[HttpGet("{ngb}/teams/{teamId}/members")]
	[Tags("Team")]
	[Authorize(AuthorizationPolicies.TeamManagerOrNgbAdminPolicy)]
	public Filtered<TeamMemberViewModel> GetTeamMembers(
		[FromRoute] NgbIdentifier ngb,
		[FromRoute] TeamIdentifier teamId,
		[FromQuery] FilteringParameters filtering)
	{
		// Get members as queryable - NGB validation is done at the lower level
		var membersQuery = this.teamContextProvider.QueryTeamMembers(teamId, NgbConstraint.Single(ngb));

		// Convert to view model
		return membersQuery
			.Select(m => new TeamMemberViewModel
			{
				UserId = m.UserId,
				Name = m.Name,
				Email = m.Email,
				PrimaryTeamName = m.PrimaryTeamName,
				PrimaryTeamId = m.PrimaryTeamId != null ? m.PrimaryTeamId.ToString() : null,
			})
			.AsFiltered();
	}

	/// <summary>
	/// List all tournament invites for a team.
	/// Team managers can view invites for their own teams.
	/// NGB admins can view invites for any team in their jurisdiction.
	/// </summary>
	[HttpGet("{ngb}/teams/{teamId}/tournamentInvites")]
	[Tags("Team")]
	[Authorize(AuthorizationPolicies.TeamManagerOrNgbAdminPolicy)]
	public async Task<IEnumerable<TournamentInviteViewModel>> GetTeamTournamentInvites(
		[FromRoute] NgbIdentifier ngb,
		[FromRoute] TeamIdentifier teamId)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();

		// Verify team exists and belongs to the specified NGB
		var team = await this.teamContextProvider.GetTeamAsync(teamId, NgbConstraint.Single(ngb));
		if (team == null)
		{
			throw new NotFoundException($"Team {teamId} not found");
		}

		// Verify user has permission (either NGB admin for this NGB, or team manager for this team)
		var hasNgbAdminPermission = userContext.Roles
			.OfType<NgbAdminRole>()
			.Any(role => role.Ngb.AppliesTo(team.NgbId));

		var hasTeamManagerPermission = userContext.Roles
			.OfType<TeamManagerRole>()
			.Any(role => role.Team.AppliesTo(teamId));

		if (!hasNgbAdminPermission && !hasTeamManagerPermission)
		{
			throw new AccessDeniedException($"No permission for team {teamId}");
		}

		// Get all tournament invites for this specific team
		var invites = await this.tournamentContextProvider.GetTeamInvitesAsync(teamId, this.HttpContext.RequestAborted);

		return invites.Select(i => new TournamentInviteViewModel
		{
			ParticipantType = i.ParticipantType,
			ParticipantId = i.ParticipantId,
			ParticipantName = i.ParticipantName,
			Status = i.GetStatus(),
			InitiatorUserId = i.InitiatorUserId,
			CreatedAt = i.CreatedAt,
			TournamentManagerApproval = new ApprovalStatusViewModel
			{
				Status = i.TournamentManagerApproval,
				Date = i.TournamentManagerApprovalDate
			},
			ParticipantApproval = new ApprovalStatusViewModel
			{
				Status = i.ParticipantApproval,
				Date = i.ParticipantApprovalDate
			}
		});
	}

	/// <summary>
	/// Updates the first and/or last name of a referee on behalf of an NGB admin.
	/// Only referees whose primary or secondary NGB matches the admin's jurisdiction can be renamed.
	/// </summary>
	[HttpPatch("{ngb}/referees/{userId}/name")]
	[Tags("Referee", "UserInfo")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> UpdateRefereeName(
		[FromRoute] NgbIdentifier ngb,
		[FromRoute] UserIdentifier userId,
		[FromBody] UpdateRefereeNameRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.FirstName) && string.IsNullOrWhiteSpace(request.LastName))
		{
			return this.BadRequest("At least one of FirstName or LastName must be provided.");
		}

		// Verify the target referee belongs to this NGB (primary or secondary)
		var belongsToNgb = await this.dbContext.Users.WithIdentifier(userId)
			.AnyAsync(u => u.RefereeLocations.Any(rl => rl.NationalGoverningBody.CountryCode == ngb.NgbCode), this.HttpContext.RequestAborted);

		if (!belongsToNgb)
		{
			return this.NotFound();
		}

		await this.updateUserDataCommand.UpdateUserDataAsync(userId, data =>
		{
			var firstName = string.IsNullOrWhiteSpace(request.FirstName) ? data.FirstName : request.FirstName;
			var lastName = string.IsNullOrWhiteSpace(request.LastName) ? data.LastName : request.LastName;
			return new ExtendedUserData(data.Email, firstName, lastName)
			{
				Bio = data.Bio,
				ExportName = data.ExportName,
				Pronouns = data.Pronouns,
				ShowPronouns = data.ShowPronouns,
				UserLang = data.UserLang,
			};
		}, this.HttpContext.RequestAborted);

		return this.NoContent();
	}

	// ──────────────────────────────────────────────────────────────────────────
	// NGB Transfer endpoints
	// ──────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// List all transfers where the origin or destination team belongs to this NGB.
	/// </summary>
	[HttpGet("{ngb}/transfers")]
	[Tags("NgbTransfers")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	public async Task<ActionResult<IEnumerable<NgbTransferViewModel>>> GetNgbTransfers(
		[FromRoute] NgbIdentifier ngb)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();
		if (!userContext.Roles.OfType<NgbAdminRole>().Any(r => r.Ngb.AppliesTo(ngb)))
		{
			return this.Forbid();
		}

		var ngbDbId = await this.dbContext.NationalGoverningBodies
			.Where(n => n.CountryCode == ngb.NgbCode)
			.Select(n => (long?)n.Id)
			.FirstOrDefaultAsync(this.HttpContext.RequestAborted);

		if (ngbDbId == null)
		{
			return this.NotFound();
		}

		var rows = await this.dbContext.NgbTransferApprovals
			.Where(a => a.NgbId == ngbDbId.Value)
			.OrderByDescending(a => a.CreatedAt)
			.Select(a => new
			{
				a.TeamInvitationId,
				a.ApprovedAt,
				a.RejectedAt,
				a.IsOriginNgb,
				a.CreatedAt,
				InvitationEmail = a.TeamInvitation.Email,
				InvitationCreatedAt = a.TeamInvitation.CreatedAt,
				IsInternalTransfer = a.TeamInvitation.IsInternalTransfer ?? false,
				InvitationAcceptedAt = a.TeamInvitation.AcceptedAt,
				InvitationDeclinedAt = a.TeamInvitation.DeclinedAt,
				InvitationRevokedAt = a.TeamInvitation.RevokedAt,
				DestinationTeamId = a.TeamInvitation.Team.Id,
				DestinationTeamName = a.TeamInvitation.Team.Name,
				OriginTeamId = (long?)a.TeamInvitation.OriginTeamId,
				OriginTeamName = a.TeamInvitation.OriginTeam != null ? a.TeamInvitation.OriginTeam.Name : null,
				PlayerUserId = (long?)null,
				PlayerUniqueId = (string?)null,
				PlayerFirstName = (string?)null,
				PlayerLastName = (string?)null,
			})
			.ToListAsync(this.HttpContext.RequestAborted);

		// Load player names by email separately to avoid complex joins.
		var emails = rows.Select(r => r.InvitationEmail.ToLower()).Distinct().ToList();
		var playersByEmail = await this.dbContext.Users
			.Where(u => emails.Contains(u.Email.ToLower()))
			.Select(u => new { Email = u.Email.ToLower(), u.FirstName, u.LastName })
			.ToDictionaryAsync(u => u.Email, this.HttpContext.RequestAborted);

		return this.Ok(rows.Select(r =>
		{
			playersByEmail.TryGetValue(r.InvitationEmail.ToLower(), out var player);
			var playerName = player != null
				? ManagementHub.Service.Areas.Teams.TeamInviteHelpers.BuildDisplayName(player.FirstName, player.LastName)
				: null;

			// Build fake approval objects just for status computation (avoids loading full EF graph).
			var fakeApprovals = new[] { new ManagementHub.Models.Data.NgbTransferApproval
			{
				ApprovedAt = r.ApprovedAt,
				RejectedAt = r.RejectedAt,
			}};

			var status = ManagementHub.Service.Areas.Teams.TeamInviteHelpers.ComputeTransferStatus(
				isTransfer: true,
				ngbApprovals: fakeApprovals,
				isAccepted: r.InvitationAcceptedAt != null,
				isDeclinedOrRevoked: r.InvitationDeclinedAt != null || r.InvitationRevokedAt != null);

			return new NgbTransferViewModel
			{
				InvitationId = new ManagementHub.Models.Domain.Team.TeamInvitationIdentifier(r.TeamInvitationId).ToString(),
				PlayerEmail = r.InvitationEmail,
				PlayerName = playerName,
				DestinationTeamId = new ManagementHub.Models.Domain.Team.TeamIdentifier(r.DestinationTeamId).ToString(),
				DestinationTeamName = r.DestinationTeamName,
				OriginTeamId = r.OriginTeamId.HasValue
					? new ManagementHub.Models.Domain.Team.TeamIdentifier(r.OriginTeamId.Value).ToString()
					: null,
				OriginTeamName = r.OriginTeamName,
				IsInternalTransfer = r.IsInternalTransfer,
				ApprovedAt = r.ApprovedAt,
				RejectedAt = r.RejectedAt,
				CreatedAt = r.InvitationCreatedAt,
				Status = status,
			};
		}));
	}

	/// <summary>
	/// Approve a player transfer on behalf of the NGB.
	/// </summary>
	[HttpPost("{ngb}/transfers/{invitationId}/approve")]
	[Tags("NgbTransfers")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	public async Task<IActionResult> ApproveNgbTransfer(
		[FromRoute] NgbIdentifier ngb,
		[FromRoute] ManagementHub.Models.Domain.Team.TeamInvitationIdentifier invitationId)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();
		if (!userContext.Roles.OfType<NgbAdminRole>().Any(r => r.Ngb.AppliesTo(ngb)))
		{
			return this.Forbid();
		}

		var ngbDbId = await this.dbContext.NationalGoverningBodies
			.Where(n => n.CountryCode == ngb.NgbCode)
			.Select(n => (long?)n.Id)
			.FirstOrDefaultAsync(this.HttpContext.RequestAborted);

		if (ngbDbId == null)
		{
			return this.NotFound();
		}

		var approval = await this.dbContext.NgbTransferApprovals
			.FirstOrDefaultAsync(
				a => a.TeamInvitationId == invitationId.Id && a.NgbId == ngbDbId.Value,
				this.HttpContext.RequestAborted);

		if (approval == null)
		{
			return this.NotFound();
		}

		if (approval.ApprovedAt != null || approval.RejectedAt != null)
		{
			return this.Conflict("Transfer has already been reviewed by this NGB.");
		}

		var currentUserDbId = await this.dbContext.Users
			.WithIdentifier(userContext.UserId)
			.Select(u => u.Id)
			.SingleAsync(this.HttpContext.RequestAborted);

		approval.ApprovedAt = DateTime.UtcNow;
		approval.ReviewedByUserId = currentUserDbId;
		await this.dbContext.SaveChangesAsync(this.HttpContext.RequestAborted);

		return this.NoContent();
	}

	/// <summary>
	/// Reject a player transfer on behalf of the NGB.
	/// </summary>
	[HttpPost("{ngb}/transfers/{invitationId}/reject")]
	[Tags("NgbTransfers")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	public async Task<IActionResult> RejectNgbTransfer(
		[FromRoute] NgbIdentifier ngb,
		[FromRoute] ManagementHub.Models.Domain.Team.TeamInvitationIdentifier invitationId)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();
		if (!userContext.Roles.OfType<NgbAdminRole>().Any(r => r.Ngb.AppliesTo(ngb)))
		{
			return this.Forbid();
		}

		var ngbDbId = await this.dbContext.NationalGoverningBodies
			.Where(n => n.CountryCode == ngb.NgbCode)
			.Select(n => (long?)n.Id)
			.FirstOrDefaultAsync(this.HttpContext.RequestAborted);

		if (ngbDbId == null)
		{
			return this.NotFound();
		}

		var approval = await this.dbContext.NgbTransferApprovals
			.FirstOrDefaultAsync(
				a => a.TeamInvitationId == invitationId.Id && a.NgbId == ngbDbId.Value,
				this.HttpContext.RequestAborted);

		if (approval == null)
		{
			return this.NotFound();
		}

		if (approval.ApprovedAt != null || approval.RejectedAt != null)
		{
			return this.Conflict("Transfer has already been reviewed by this NGB.");
		}

		var currentUserDbId = await this.dbContext.Users
			.WithIdentifier(userContext.UserId)
			.Select(u => u.Id)
			.SingleAsync(this.HttpContext.RequestAborted);

		approval.RejectedAt = DateTime.UtcNow;
		approval.ReviewedByUserId = currentUserDbId;

		// Also revoke the team invitation so the player is not left in limbo.
		var invitation = await this.dbContext.TeamInvitations
			.FirstOrDefaultAsync(
				i => i.Id == invitationId.Id
					&& i.RevokedAt == null
					&& i.AcceptedAt == null
					&& i.DeclinedAt == null,
				this.HttpContext.RequestAborted);

		if (invitation != null)
		{
			invitation.RevokedAt = DateTime.UtcNow;
			this.dbContext.TeamPlayerActivities.Add(new ManagementHub.Models.Data.TeamPlayerActivity
			{
				TeamId = invitation.TeamId,
				Email = invitation.Email,
				InitiatorUserId = currentUserDbId,
				ActivityType = ManagementHub.Models.Enums.TeamPlayerActivityType.InviteRevoked,
				CreatedAt = DateTime.UtcNow,
			});
		}

		await this.dbContext.SaveChangesAsync(this.HttpContext.RequestAborted);

		return this.NoContent();
	}

	/// <summary>
	/// Update NGB transfer settings (auto-approve internal transfers).
	/// </summary>
	[HttpPut("{ngb}/settings/transfers")]
	[Tags("NgbTransfers")]
	[Authorize(AuthorizationPolicies.NgbAdminPolicy)]
	public async Task<IActionResult> UpdateNgbTransferSettings(
		[FromRoute] NgbIdentifier ngb,
		[FromBody] NgbTransferSettingsRequest request)
	{
		var userContext = await this.contextAccessor.GetCurrentUserContextAsync();
		if (!userContext.Roles.OfType<NgbAdminRole>().Any(r => r.Ngb.AppliesTo(ngb)))
		{
			return this.Forbid();
		}

		var ngbEntity = await this.dbContext.NationalGoverningBodies
			.FirstOrDefaultAsync(n => n.CountryCode == ngb.NgbCode, this.HttpContext.RequestAborted);

		if (ngbEntity == null)
		{
			return this.NotFound();
		}

		ngbEntity.AutoApproveInternalTransfers = request.AutoApproveInternalTransfers;
		await this.dbContext.SaveChangesAsync(this.HttpContext.RequestAborted);

		return this.NoContent();
	}
}
