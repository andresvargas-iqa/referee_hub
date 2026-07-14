import { useMemo } from "react";
import { TournamentViewModel } from "../../../store/serviceApi";
import { TournamentData } from "../components/TournamentsSection";

const OLDER_TOURNAMENT_DAYS = 30;
const ONE_DAY_IN_MS = 24 * 60 * 60 * 1000;

export const convertToDisplayFormat = (t: TournamentViewModel): TournamentData => ({
  id: t.id,
  title: t.name || "",
  description: t.description || "",
  startDate: t.startDate || "",
  endDate: t.endDate || "",
  type: t.type,
  country: t.country || "",
  location: [t.place, t.city].filter(Boolean).join(", "),
  bannerImageUrl: t.bannerImageUrl || undefined,
  organizer: t.organizer || undefined,
  isPrivate: Boolean(t.isCurrentUserInvolved),
});

export const applyTypeFilter = (tournaments: TournamentViewModel[], typeFilter: string): TournamentViewModel[] => {
  if (!typeFilter) {
    return tournaments;
  }
  return tournaments.filter((t) => t.type === typeFilter);
};

export const applyOlderTournamentsFilter = (
  tournaments: TournamentViewModel[],
  showOlderTournaments: boolean,
  now: Date = new Date()
): TournamentViewModel[] => {
  if (showOlderTournaments) {
    return tournaments;
  }

  const cutoffTimestamp = now.getTime() - OLDER_TOURNAMENT_DAYS * ONE_DAY_IN_MS;

  return tournaments.filter((tournament) => {
    if (!tournament.endDate) {
      return true;
    }

    const endDateTimestamp = Date.parse(tournament.endDate);
    if (Number.isNaN(endDateTimestamp)) {
      return true;
    }

    return endDateTimestamp >= cutoffTimestamp;
  });
};

export const calculatePublicTournamentCount = (
  allTournaments: TournamentViewModel[],
  typeFilter: string
): number => {
  const filtered = applyTypeFilter(allTournaments, typeFilter);
  return filtered.filter((t) => !t.isCurrentUserInvolved).length;
};

interface TournamentSections {
  publicTournaments: TournamentData[];
  privateTournaments: TournamentData[];
  totalCount: number;
}

export const useTournamentSections = (
  isAnonymous: boolean,
  filteredAllTournaments: TournamentViewModel[],
  filteredPaginatedTournaments: TournamentViewModel[]
): TournamentSections => {
  return useMemo(() => {
    if (isAnonymous) {
      return {
        publicTournaments: filteredPaginatedTournaments.map((t) => convertToDisplayFormat({
          ...t,
          isCurrentUserInvolved: false,
        })),
        privateTournaments: [],
        totalCount: filteredAllTournaments.length,
      };
    }

    // Private tournaments come from the unpaginated query (all tournaments)
    const userInvolvedTournaments = filteredAllTournaments
      .filter((t) => t.isCurrentUserInvolved)
      .map(convertToDisplayFormat);

    // Public tournaments come from the paginated query
    const otherTournaments = filteredPaginatedTournaments
      .filter((t) => !t.isCurrentUserInvolved)
      .map(convertToDisplayFormat);

    // Calculate public tournament count from all tournaments (for correct pagination)
    const publicTournamentCount = filteredAllTournaments.filter(
      (t) => !t.isCurrentUserInvolved
    ).length;

    return {
      publicTournaments: otherTournaments,
      privateTournaments: userInvolvedTournaments,
      totalCount: publicTournamentCount,
    };
  }, [isAnonymous, filteredAllTournaments, filteredPaginatedTournaments]);
};
