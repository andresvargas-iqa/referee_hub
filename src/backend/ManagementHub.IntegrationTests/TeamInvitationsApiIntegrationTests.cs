using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using ManagementHub.IntegrationTests.Helpers;
using ManagementHub.IntegrationTests.Models;
using Xunit;

namespace ManagementHub.IntegrationTests;

public class TeamInvitationsApiIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
	private readonly TestWebApplicationFactory factory;
	private readonly HttpClient client;

	public TeamInvitationsApiIntegrationTests(TestWebApplicationFactory factory)
	{
		this.factory = factory;
		this.client = factory.CreateClient();
	}

	[Fact]
	public async Task InvitePlayer_AsTeamManager_ShouldCreatePendingInvite()
	{
		await AuthenticationHelper.AuthenticateAsAsync(this.client, "team_manager@example.com", "password");

		var response = await this.client.PostAsJsonAsync("/api/v2/Teams/TM_1/invites", new
		{
			Email = "invitee@example.com"
		});

		response.StatusCode.Should().Be(HttpStatusCode.Created);

		var invite = await response.Content.ReadFromJsonAsync<TeamInvitationViewModelDto>();
		invite.Should().NotBeNull();
		invite!.Email.Should().Be("invitee@example.com");

		var managementResponse = await this.client.GetAsync("/api/v2/Teams/TM_1/management");
		managementResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var team = await managementResponse.Content.ReadFromJsonAsync<TeamManagementViewModelDto>();
		team.Should().NotBeNull();
		team!.PendingInvites.Should().Contain(i => i.Email == "invitee@example.com");
	}

	[Fact]
	public async Task InvitePlayer_DuplicatePendingInvite_ShouldReturnBadRequest()
	{
		await AuthenticationHelper.AuthenticateAsAsync(this.client, "team_manager@example.com", "password");

		await this.client.PostAsJsonAsync("/api/v2/Teams/TM_1/invites", new
		{
			Email = "duplicate@example.com"
		});

		var response = await this.client.PostAsJsonAsync("/api/v2/Teams/TM_1/invites", new
		{
			Email = "duplicate@example.com"
		});

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task InvitePlayer_ForExistingMember_ShouldReturnBadRequest()
	{
		await AuthenticationHelper.AuthenticateAsAsync(this.client, "team_manager@example.com", "password");

		var response = await this.client.PostAsJsonAsync("/api/v2/Teams/TM_1/invites", new
		{
			Email = "sarah.player@example.com"
		});

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task RevokeInvite_ShouldRemoveInviteFromManagementView()
	{
		await AuthenticationHelper.AuthenticateAsAsync(this.client, "team_manager@example.com", "password");

		var createResponse = await this.client.PostAsJsonAsync("/api/v2/Teams/TM_1/invites", new
		{
			Email = "revoke.me@example.com"
		});

		var invite = await createResponse.Content.ReadFromJsonAsync<TeamInvitationViewModelDto>();
		invite.Should().NotBeNull();

		var revokeResponse = await this.client.DeleteAsync($"/api/v2/Teams/TM_1/invites/{invite!.InvitationId}");
		revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var managementResponse = await this.client.GetAsync("/api/v2/Teams/TM_1/management");
		var team = await managementResponse.Content.ReadFromJsonAsync<TeamManagementViewModelDto>();
		team!.PendingInvites.Should().NotContain(i => i.Email == "revoke.me@example.com");
	}

	[Fact]
	public async Task InvitePlayer_AsRegularUser_ShouldReturnForbidden()
	{
		await AuthenticationHelper.AuthenticateAsAsync(this.client, "sarah.player@example.com", "password");

		var response = await this.client.PostAsJsonAsync("/api/v2/Teams/TM_1/invites", new
		{
			Email = "blocked@example.com"
		});

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task RespondToInvite_Accept_ShouldCreateMembershipAndHistory()
	{
		this.factory.EmailSender.Clear();

		await AuthenticationHelper.AuthenticateAsAsync(this.client, "team_manager@example.com", "password");

		var createResponse = await this.client.PostAsJsonAsync("/api/v2/Teams/TM_1/invites", new
		{
			Email = "ngb_admin@example.com"
		});

		createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		var invite = await createResponse.Content.ReadFromJsonAsync<TeamInvitationViewModelDto>();
		invite.Should().NotBeNull();

		await AuthenticationHelper.AuthenticateAsAsync(this.client, "ngb_admin@example.com", "password");

		var myInvitesResponse = await this.client.GetAsync("/api/v2/users/me/teamInvites");
		myInvitesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var myInvites = await myInvitesResponse.Content.ReadFromJsonAsync<List<CurrentUserTeamInviteViewModelDto>>();
		myInvites.Should().NotBeNull();
		myInvites!.Should().Contain(i => i.InvitationId == invite!.InvitationId && i.TeamId == "TM_1");

		var respondResponse = await this.client.PostAsJsonAsync(
			$"/api/v2/users/me/teamInvites/{invite!.InvitationId}",
			new { Approved = true });

		respondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var myInvitesAfterResponse = await this.client.GetAsync("/api/v2/users/me/teamInvites");
		myInvitesAfterResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var pendingAfterResponse = await myInvitesAfterResponse.Content.ReadFromJsonAsync<List<CurrentUserTeamInviteViewModelDto>>();
		pendingAfterResponse.Should().NotBeNull();
		pendingAfterResponse!.Should().NotContain(i => i.InvitationId == invite.InvitationId);

		await AuthenticationHelper.AuthenticateAsAsync(this.client, "team_manager@example.com", "password");

		var managementResponse = await this.client.GetAsync("/api/v2/Teams/TM_1/management");
		managementResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var team = await managementResponse.Content.ReadFromJsonAsync<TeamManagementViewModelDto>();
		team.Should().NotBeNull();
		team!.PendingInvites.Should().NotContain(i => i.InvitationId == invite.InvitationId);
		team.Members.Should().Contain(m => m.Email == "ngb_admin@example.com");
		team.PlayerHistory.Should().Contain(a => a.ActivityType == "inviteAccepted" && a.Email == "ngb_admin@example.com");

		var sentEmails = this.factory.EmailSender.GetSentEmails();
		sentEmails.Should().Contain(e => e.Subject.Contains("Team Invitation accepted") && e.To.Contains("team_manager@example.com"));
	}

	[Fact]
	public async Task RespondToInvite_Decline_ShouldCloseInviteAndRecordHistory()
	{
		this.factory.EmailSender.Clear();

		await AuthenticationHelper.AuthenticateAsAsync(this.client, "team_manager@example.com", "password");

		var createResponse = await this.client.PostAsJsonAsync("/api/v2/Teams/TM_1/invites", new
		{
			Email = "team_manager@example.com"
		});

		createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		var invite = await createResponse.Content.ReadFromJsonAsync<TeamInvitationViewModelDto>();
		invite.Should().NotBeNull();

		var respondResponse = await this.client.PostAsJsonAsync(
			$"/api/v2/users/me/teamInvites/{invite!.InvitationId}",
			new { Approved = false });

		respondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var myInvitesResponse = await this.client.GetAsync("/api/v2/users/me/teamInvites");
		myInvitesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var myInvites = await myInvitesResponse.Content.ReadFromJsonAsync<List<CurrentUserTeamInviteViewModelDto>>();
		myInvites.Should().NotBeNull();
		myInvites!.Should().NotContain(i => i.InvitationId == invite.InvitationId);

		var managementResponse = await this.client.GetAsync("/api/v2/Teams/TM_1/management");
		managementResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var team = await managementResponse.Content.ReadFromJsonAsync<TeamManagementViewModelDto>();
		team.Should().NotBeNull();
		team!.PendingInvites.Should().NotContain(i => i.InvitationId == invite.InvitationId);
		team.PlayerHistory.Should().Contain(a => a.ActivityType == "inviteDeclined" && a.Email == "team_manager@example.com");

		var sentEmails = this.factory.EmailSender.GetSentEmails();
		sentEmails.Should().Contain(e => e.Subject.Contains("Team Invitation declined") && e.To.Contains("team_manager@example.com"));
	}
}