using System.Collections.Generic;

namespace ManagementHub.IntegrationTests.Models;

public class TeamManagementViewModelDto
{
	public List<TeamInvitationViewModelDto> PendingInvites { get; set; } = [];
}