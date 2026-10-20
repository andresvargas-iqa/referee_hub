import React, { useEffect, useMemo, useState } from "react";
import { Dialog, DialogPanel, DialogTitle } from "@headlessui/react";
import {
  useGetParticipantsQuery,
  useGetTournamentRankingsQuery,
  useSaveTournamentRankingsMutation,
} from "../../../store/serviceApi";

type RankingRow = {
  teamId: string;
  teamName: string;
  rankingPosition: number;
};

type TournamentRankingModalProps = {
  isOpen: boolean;
  tournamentId: string;
  onClose: () => void;
};

const TournamentRankingModal = ({ isOpen, tournamentId, onClose }: TournamentRankingModalProps) => {
  const [rankings, setRankings] = useState<RankingRow[]>([]);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [draggingTeamId, setDraggingTeamId] = useState<string | null>(null);

  const { data: participants } = useGetParticipantsQuery(
    { tournamentId },
    { skip: !isOpen || !tournamentId }
  );
  const { data: existingRankings, refetch: refetchRankings } = useGetTournamentRankingsQuery(
    { tournamentId },
    { skip: !isOpen || !tournamentId }
  );
  const [saveTournamentRankings, { isLoading: isSaving }] = useSaveTournamentRankingsMutation();

  const participantTeams = useMemo(() => {
    if (!participants) {
      return [];
    }

    return participants
      .filter((p) => p.teamId && p.teamName)
      .map((p) => ({ teamId: p.teamId as string, teamName: p.teamName as string }));
  }, [participants]);

  useEffect(() => {
    if (!isOpen || participantTeams.length === 0) {
      return;
    }

    if (existingRankings && existingRankings.length > 0) {
      const rankingByTeamId = new Map(
        existingRankings
          .filter((r) => r.teamId && r.rankingPosition)
          .map((r) => [r.teamId as string, r.rankingPosition as number])
      );

      const sorted = [...participantTeams]
        .map((team) => ({
          ...team,
          rankingPosition: rankingByTeamId.get(team.teamId) ?? Number.MAX_SAFE_INTEGER,
        }))
        .sort((a, b) => {
          if (a.rankingPosition === b.rankingPosition) {
            return a.teamName.localeCompare(b.teamName);
          }
          return a.rankingPosition - b.rankingPosition;
        })
        .map((team, index) => ({
          ...team,
          rankingPosition: index + 1,
        }));

      setRankings(sorted);
      return;
    }

    const initialRankings = [...participantTeams]
      .sort((a, b) => a.teamName.localeCompare(b.teamName))
      .map((team, index) => ({
        ...team,
        rankingPosition: index + 1,
      }));

    setRankings(initialRankings);
  }, [isOpen, existingRankings, participantTeams]);

  const moveTeam = (index: number, direction: "up" | "down") => {
    const targetIndex = direction === "up" ? index - 1 : index + 1;
    if (targetIndex < 0 || targetIndex >= rankings.length) {
      return;
    }

    const updated = [...rankings];
    const current = updated[index];
    updated[index] = updated[targetIndex];
    updated[targetIndex] = current;

    setRankings(
      updated.map((team, i) => ({
        ...team,
        rankingPosition: i + 1,
      }))
    );
  };

  const handleDropOnIndex = (toIndex: number) => {
    if (!draggingTeamId) {
      return;
    }

    const fromIndex = rankings.findIndex((r) => r.teamId === draggingTeamId);
    if (fromIndex < 0 || fromIndex === toIndex) {
      setDraggingTeamId(null);
      return;
    }

    const updated = [...rankings];
    const [moved] = updated.splice(fromIndex, 1);
    updated.splice(toIndex, 0, moved);

    setRankings(
      updated.map((team, i) => ({
        ...team,
        rankingPosition: i + 1,
      }))
    );
    setDraggingTeamId(null);
  };

  const handleSave = async () => {
    setSaveError(null);

    try {
      await saveTournamentRankings({
        tournamentId,
        saveRankingsRequest: {
          rankings: rankings.map((row) => ({
            teamId: row.teamId,
            rankingPosition: row.rankingPosition,
          })),
        },
      }).unwrap();

      refetchRankings();
      onClose();
    } catch {
      setSaveError("Failed to save rankings. Please try again.");
    }
  };

  return (
    <Dialog open={isOpen} as="div" className="relative z-50" onClose={onClose}>
      <div className="fixed inset-0 bg-black/30" aria-hidden="true" />
      <div className="fixed inset-0 flex items-center justify-center p-4 overflow-y-auto">
        <DialogPanel className="w-full max-w-3xl rounded-xl bg-white shadow-xl">
          <div className="p-6 border-b border-gray-200 flex items-center justify-between">
            <DialogTitle as="h3" className="text-xl font-semibold text-gray-900">
              Tournament Ranking
            </DialogTitle>
            <button onClick={onClose} className="text-gray-500 hover:text-gray-700" aria-label="Close ranking modal">
              ×
            </button>
          </div>

          <div className="p-6">
            <p className="text-sm text-gray-600 mb-4">
              Reorder teams to define the final standings. Rankings can be edited later.
            </p>

            {rankings.length === 0 ? (
              <p className="text-gray-500">No participating teams found for this tournament.</p>
            ) : (
              <div className="space-y-2">
                {rankings.map((row, index) => (
                  <div
                    key={row.teamId}
                    className="flex items-center justify-between border border-gray-200 rounded-lg px-4 py-3"
                    draggable
                    onDragStart={() => setDraggingTeamId(row.teamId)}
                    onDragEnd={() => setDraggingTeamId(null)}
                    onDragOver={(event) => event.preventDefault()}
                    onDrop={() => handleDropOnIndex(index)}
                    style={{
                      cursor: "grab",
                      opacity: draggingTeamId === row.teamId ? 0.45 : 1,
                    }}
                  >
                    <div className="flex items-center gap-4">
                      <span className="text-gray-400" aria-hidden="true">::</span>
                      <span className="font-semibold text-gray-900 w-8">#{row.rankingPosition}</span>
                      <span className="text-gray-800">{row.teamName}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        onClick={() => moveTeam(index, "up")}
                        disabled={index === 0}
                        className="px-3 py-1.5 text-sm border border-gray-300 rounded disabled:opacity-40"
                      >
                        Up
                      </button>
                      <button
                        type="button"
                        onClick={() => moveTeam(index, "down")}
                        disabled={index === rankings.length - 1}
                        className="px-3 py-1.5 text-sm border border-gray-300 rounded disabled:opacity-40"
                      >
                        Down
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}

            {saveError && <p className="text-sm text-red-600 mt-4">{saveError}</p>}
          </div>

          <div className="p-6 border-t border-gray-200 flex justify-end gap-3">
            <button onClick={onClose} className="px-4 py-2 border border-gray-300 rounded-md text-gray-700">
              Cancel
            </button>
            <button
              onClick={handleSave}
              disabled={isSaving || rankings.length === 0}
              className="px-4 py-2 bg-green text-white rounded-md disabled:opacity-50"
            >
              {isSaving ? "Saving..." : "Save Rankings"}
            </button>
          </div>
        </DialogPanel>
      </div>
    </Dialog>
  );
};

export default TournamentRankingModal;
