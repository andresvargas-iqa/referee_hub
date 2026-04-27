using System;

namespace ManagementHub.IntegrationTests.Models;

public class TeamInvitationViewModelDto
{
	public required string InvitationId { get; set; }
	public required string Email { get; set; }
	public required DateTime CreatedAt { get; set; }
	public string? InvitedByName { get; set; }
}

public class CurrentUserTeamInviteViewModelDto
{
	public required string InvitationId { get; set; }
	public required string TeamId { get; set; }
	public required string TeamName { get; set; }
	public required string Email { get; set; }
	public required DateTime CreatedAt { get; set; }
	public string? InvitedByName { get; set; }
}