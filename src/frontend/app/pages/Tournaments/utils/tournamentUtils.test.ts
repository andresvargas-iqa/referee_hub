import { applyOlderTournamentsFilter } from "./tournamentUtils";
import { TournamentViewModel } from "../../../store/serviceApi";

describe("applyOlderTournamentsFilter", () => {
  const now = new Date("2026-07-14T12:00:00.000Z");

  const createTournament = (endDate?: string): TournamentViewModel => ({
    id: Math.random().toString(),
    name: "Tournament",
    endDate,
  });

  it("hides tournaments with an end date older than 30 days", () => {
    const tournaments: TournamentViewModel[] = [
      createTournament("2026-06-13T00:00:00.000Z"),
      createTournament("2026-06-14T00:00:00.000Z"),
      createTournament("2026-06-20T00:00:00.000Z"),
    ];

    const filtered = applyOlderTournamentsFilter(tournaments, false, now);

    expect(filtered).toHaveLength(2);
    expect(filtered[0].endDate).toBe("2026-06-14T00:00:00.000Z");
    expect(filtered[1].endDate).toBe("2026-06-20T00:00:00.000Z");
  });

  it("keeps all tournaments when show older tournaments is enabled", () => {
    const tournaments: TournamentViewModel[] = [
      createTournament("2026-06-01T00:00:00.000Z"),
      createTournament("2026-06-20T00:00:00.000Z"),
    ];

    const filtered = applyOlderTournamentsFilter(tournaments, true, now);

    expect(filtered).toEqual(tournaments);
  });

  it("keeps tournaments when end date is missing or invalid", () => {
    const tournaments: TournamentViewModel[] = [
      createTournament(undefined),
      createTournament("not-a-date"),
      createTournament("2026-06-01T00:00:00.000Z"),
    ];

    const filtered = applyOlderTournamentsFilter(tournaments, false, now);

    expect(filtered).toHaveLength(2);
    expect(filtered[0].endDate).toBeUndefined();
    expect(filtered[1].endDate).toBe("not-a-date");
  });
});
