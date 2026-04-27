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
	private readonly HttpClient client;

	public TeamInvitationsApiIntegrationTests(TestWebApplicationFactory factory)
	{
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
}