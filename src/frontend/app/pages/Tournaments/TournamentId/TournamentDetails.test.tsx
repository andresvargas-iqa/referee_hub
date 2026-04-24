import React from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import TournamentDetails from "./TournamentDetails";

const mockUseNavigationParams = jest.fn();
const mockUseNavigate = jest.fn();

const mockUseGetTournamentQuery = jest.fn();
const mockUseGetTournamentManagersQuery = jest.fn();
const mockUseGetCurrentUserQuery = jest.fn();
const mockUseGetTournamentInvitesQuery = jest.fn();
const mockUseRespondToInviteMutation = jest.fn();
const mockUseGetManagedTeamsQuery = jest.fn();
const mockUseGetParticipantsQuery = jest.fn();
const mockUseDeleteTournamentMutation = jest.fn();

function mockForwardRefComponent(testId: string, displayName: string) {
  const Component = React.forwardRef(() => <div data-testid={testId} />);
  Component.displayName = displayName;
  return Component;
}

function mockAddTournamentManagerModal() {
  return <div data-testid="add-manager-modal" />;
}
mockAddTournamentManagerModal.displayName = "MockAddTournamentManagerModal";

function mockTournamentRankingModal(props: { isOpen: boolean }) {
  return props.isOpen ? <div data-testid="tournament-ranking-modal">Tournament Ranking Modal</div> : null;
}
mockTournamentRankingModal.displayName = "MockTournamentRankingModal";

jest.mock("../../../utils/navigationUtils", () => ({
  useNavigationParams: () => mockUseNavigationParams(),
  useNavigate: () => mockUseNavigate,
}));

jest.mock("../../../hooks/useAlert", () => ({
  useAlert: () => ({
    alertState: { isVisible: false, message: "", type: "success" },
    showAlert: jest.fn(),
    hideAlert: jest.fn(),
  }),
}));

jest.mock("../../../store/serviceApi", () => ({
  useGetTournamentQuery: (...args: unknown[]) => mockUseGetTournamentQuery(...args),
  useGetTournamentManagersQuery: (...args: unknown[]) => mockUseGetTournamentManagersQuery(...args),
  useGetCurrentUserQuery: (...args: unknown[]) => mockUseGetCurrentUserQuery(...args),
  useGetTournamentInvitesQuery: (...args: unknown[]) => mockUseGetTournamentInvitesQuery(...args),
  useRespondToInviteMutation: (...args: unknown[]) => mockUseRespondToInviteMutation(...args),
  useGetManagedTeamsQuery: (...args: unknown[]) => mockUseGetManagedTeamsQuery(...args),
  useGetParticipantsQuery: (...args: unknown[]) => mockUseGetParticipantsQuery(...args),
  useDeleteTournamentMutation: (...args: unknown[]) => mockUseDeleteTournamentMutation(...args),
}));

jest.mock("./components", () => ({
  TournamentHeader: () => <div data-testid="tournament-header" />,
  TournamentInfoCards: () => <div data-testid="tournament-info-cards" />,
  TournamentAboutSection: () => <div data-testid="tournament-about" />,
  RosterManager: () => <div data-testid="roster-manager" />,
}));

jest.mock("./RegisterTournamentModal", () => mockForwardRefComponent("register-modal", "MockRegisterTournamentModal"));

jest.mock("./ContactOrganizerModal", () =>
  mockForwardRefComponent("contact-organizer-modal", "MockContactOrganizerModal")
);

jest.mock("../components/AddTournamentModal", () =>
  mockForwardRefComponent("add-tournament-modal", "MockAddTournamentModal")
);

jest.mock("./RegistrationsModal", () =>
  mockForwardRefComponent("registrations-modal", "MockRegistrationsModal")
);

jest.mock("./InviteTeamsModal", () =>
  mockForwardRefComponent("invite-teams-modal", "MockInviteTeamsModal")
);

jest.mock("./AddTournamentManagerModal", () => mockAddTournamentManagerModal);

jest.mock("./TournamentRankingModal", () => mockTournamentRankingModal);

const setupDefaultMocks = (endDate: string) => {
  mockUseNavigationParams.mockReturnValue({ tournamentId: "TR_test" });

  mockUseGetTournamentQuery.mockReturnValue({
    data: {
      id: "TR_test",
      name: "Test Tournament",
      description: "A tournament",
      startDate: "2026-04-23",
      endDate,
      country: "USA",
      city: "Boston",
      organizer: "Org",
      type: "Club",
      isPrivate: false,
      isRegistrationOpen: true,
    },
    isLoading: false,
    isError: false,
  });

  mockUseGetCurrentUserQuery.mockReturnValue({
    data: {
      userId: "U_1",
      roles: [{ roleType: "TournamentManager", tournament: ["TR_test"] }],
    },
  });

  mockUseGetTournamentManagersQuery.mockReturnValue({
    data: [{ id: "U_1" }],
    isError: false,
  });

  mockUseGetTournamentInvitesQuery.mockReturnValue({
    data: [],
    refetch: jest.fn(),
  });

  mockUseGetManagedTeamsQuery.mockReturnValue({ data: [] });

  mockUseGetParticipantsQuery.mockReturnValue({
    data: [],
    refetch: jest.fn(),
  });

  mockUseRespondToInviteMutation.mockReturnValue([jest.fn()]);
  mockUseDeleteTournamentMutation.mockReturnValue([jest.fn()]);
};

const formatDateOnly = (date: Date): string => {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, "0");
  const day = `${date.getDate()}`.padStart(2, "0");
  return `${year}-${month}-${day}`;
};

describe("TournamentDetails ranking button", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("shows enabled Tournament Ranking button for finished tournaments", () => {
    setupDefaultMocks(formatDateOnly(new Date()));

    render(<TournamentDetails />);

    const rankingButton = screen.getByRole("button", { name: "Tournament Ranking" });
    expect(rankingButton).toBeInTheDocument();
    expect(rankingButton).toBeEnabled();
  });

  it("opens ranking modal when Tournament Ranking button is clicked", async () => {
    setupDefaultMocks(formatDateOnly(new Date()));

    render(<TournamentDetails />);

    await userEvent.click(screen.getByRole("button", { name: "Tournament Ranking" }));

    expect(screen.getByTestId("tournament-ranking-modal")).toBeInTheDocument();
  });

  it("shows disabled Tournament Ranking button with helper text before tournament ends", () => {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    setupDefaultMocks(formatDateOnly(tomorrow));

    render(<TournamentDetails />);

    const rankingButton = screen.getByRole("button", { name: "Tournament Ranking" });
    expect(rankingButton).toBeDisabled();
    expect(
      screen.getByText("Ranking becomes available after the tournament end date.")
    ).toBeInTheDocument();
  });
});
