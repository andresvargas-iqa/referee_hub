using System.Collections.Generic;
using ManagementHub.Models.Domain.Team;

namespace ManagementHub.Service.Areas.Tournaments;

public class SaveRankingsRequest
{
	public required List<RankingEntry> Rankings { get; set; }

	public class RankingEntry
	{
		public required TeamIdentifier TeamId { get; set; }
		public int RankingPosition { get; set; }
	}
}
