using System;
using ManagementHub.Models.Abstraction;

namespace ManagementHub.Models.Data;

public partial class TournamentTeamRanking : IIdentifiable
{
	public long Id { get; set; }
	public long TournamentId { get; set; }
	public long TeamId { get; set; }
	public int RankingPosition { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public DateTime? DeletedAt { get; set; }

	public virtual Tournament Tournament { get; set; } = null!;
	public virtual Team Team { get; set; } = null!;
}
