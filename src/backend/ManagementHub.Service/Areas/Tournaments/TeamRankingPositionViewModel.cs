using ManagementHub.Models.Domain.Tournament;

namespace ManagementHub.Service.Areas.Tournaments;

public class TeamRankingPositionViewModel
{
	public required TournamentIdentifier TournamentId { get; set; }
	public required string TournamentName { get; set; }
	public int RankingPosition { get; set; }
}
