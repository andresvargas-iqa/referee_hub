import React from "react";
import { Link } from "react-router-dom";
import { useGetTeamTournamentRankingsQuery } from "../store/serviceApi";

type TeamTournamentRankingsSectionProps = {
  teamId: string;
};

const TeamTournamentRankingsSection = ({ teamId }: TeamTournamentRankingsSectionProps) => {
  const { data: rankings, isLoading, isError } = useGetTeamTournamentRankingsQuery(
    { teamId },
    { skip: !teamId }
  );

  return (
    <div className="bg-gray-100 rounded-lg p-4 mb-8">
      <h2 className="text-xl font-semibold mb-2">Tournament Rankings</h2>

      {isLoading && <p className="text-gray-500">Loading rankings...</p>}
      {isError && <p className="text-red-500">Could not load tournament rankings.</p>}
      {!isLoading && !isError && (!rankings || rankings.length === 0) && (
        <p className="text-gray-500">No tournament rankings yet.</p>
      )}

      {!isLoading && !isError && rankings && rankings.length > 0 && (
        <ul className="space-y-2">
          {rankings.map((ranking) => (
            <li key={`${ranking.tournamentId}-${ranking.rankingPosition}`} className="bg-white rounded p-3 border border-gray-200">
              <div className="flex items-center justify-between gap-4">
                <Link
                  to={`/tournaments/${ranking.tournamentId}`}
                  className="text-blue-600 hover:underline font-medium"
                >
                  {ranking.tournamentName}
                </Link>
                <span className="font-semibold text-gray-800">#{ranking.rankingPosition}</span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

export default TeamTournamentRankingsSection;
