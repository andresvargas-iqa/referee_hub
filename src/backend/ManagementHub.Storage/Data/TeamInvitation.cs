using System;
using ManagementHub.Models.Abstraction;

namespace ManagementHub.Models.Data;

public partial class TeamInvitation : IIdentifiable
{
	public long Id { get; set; }
	public long TeamId { get; set; }
	public string Email { get; set; } = null!;
	public long InitiatorUserId { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? RevokedAt { get; set; }

	public virtual Team Team { get; set; } = null!;
	public virtual User Initiator { get; set; } = null!;
}