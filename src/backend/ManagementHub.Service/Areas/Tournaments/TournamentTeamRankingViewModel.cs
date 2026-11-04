using ManagementHub.Models.Domain.Tournament;
using ManagementHub.Models.Domain.Team;

namespace ManagementHub.Service.Areas.Tournaments;

public class TournamentTeamRankingViewModel
{
	public required TeamIdentifier TeamId { get; set; }
	public required string TeamName { get; set; }
	public int RankingPosition { get; set; }
}
