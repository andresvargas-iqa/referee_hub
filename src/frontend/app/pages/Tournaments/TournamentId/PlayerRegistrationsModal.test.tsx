import React from "react";
import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import PlayerRegistrationsModal, { PlayerRegistrationsModalRef } from "./PlayerRegistrationsModal";

const mockRespond = jest.fn();
const mockDelete = jest.fn();
const mockInvites = [
  { participantType: "team", participantId: "TM_1", participantName: "Team Entry", status: "pending" },
  { participantType: "referee", participantId: "UR_1", participantName: "Volunteer Entry", status: "pending" },
  { participantType: "player", participantId: "UR_1", participantName: "Player Entry", status: "pending", tournamentManagerApproval: { status: "pending" } },
];

jest.mock("../../../store/serviceApi", () => ({
  useGetTournamentInvitesQuery: () => ({ data: mockInvites, isFetching: false, isError: false }),
  useRespondToInviteMutation: () => [mockRespond, { isLoading: false }],
  useDeleteInviteMutation: () => [mockDelete, { isLoading: false }],
}));

function openModal() {
  const ref = React.createRef<PlayerRegistrationsModalRef>();
  render(<PlayerRegistrationsModal ref={ref} />);
  act(() => ref.current?.open("TR_1", "Fantasy Cup"));
}

describe("PlayerRegistrationsModal", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockRespond.mockReturnValue({ unwrap: () => Promise.resolve() });
    mockDelete.mockReturnValue({ unwrap: () => Promise.resolve() });
  });

  it("shows only player registrations alongside team and volunteer entries", () => {
    openModal();
    expect(screen.getByText("Player Entry")).toBeInTheDocument();
    expect(screen.queryByText("Team Entry")).not.toBeInTheDocument();
    expect(screen.queryByText("Volunteer Entry")).not.toBeInTheDocument();
  });

  it.each([["Accept", true], ["Decline", false]])("targets player registrations when selecting %s", async (label, approved) => {
    openModal();
    fireEvent.click(screen.getByRole("button", { name: label as string }));
    await waitFor(() => expect(mockRespond).toHaveBeenCalledWith({
      tournamentId: "TR_1",
      participantId: "UR_1",
      participantType: "player",
      inviteResponseModel: { approved },
    }));
  });

  it("removes a registration after confirmation", async () => {
    const confirmSpy = jest.spyOn(window, "confirm").mockReturnValue(true);
    openModal();
    fireEvent.click(screen.getByRole("button", { name: "Remove Player Entry's registration" }));
    await waitFor(() => expect(mockDelete).toHaveBeenCalledWith({ tournamentId: "TR_1", participantId: "UR_1" }));
    confirmSpy.mockRestore();
  });
});