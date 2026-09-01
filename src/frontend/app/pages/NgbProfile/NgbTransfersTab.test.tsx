import { fireEvent, render, screen } from "@testing-library/react";
import React from "react";

import { useGetNgbTransfersQuery } from "../../store/serviceApi";
import NgbTransfersTab from "./NgbTransfersTab";

jest.mock("../../store/serviceApi", () => ({
  useApproveNgbTransferMutation: () => [jest.fn()],
  useGetNgbTransfersQuery: jest.fn(),
  useRejectNgbTransferMutation: () => [jest.fn()],
  useUpdateNgbTransferSettingsMutation: () => [jest.fn(), { isLoading: false }],
}));

const mockUseGetNgbTransfersQuery = useGetNgbTransfersQuery as jest.Mock;

describe("NgbTransfersTab", () => {
  beforeEach(() => {
    mockUseGetNgbTransfersQuery.mockReturnValue({
      data: {
        items: [{ invitationId: "TI_1", playerName: "Player One", status: "pendingNgbApproval" }],
        metadata: { totalCount: 26 },
      },
      error: undefined,
      isLoading: false,
    });
  });

  it("requests and navigates through 25-row pages", () => {
    render(<NgbTransfersTab ngbId="USA" />);

    expect(screen.getByText("Pending NGB approval")).toHaveClass("border-yellow-300", "bg-yellow-50", "text-yellow-800");

    fireEvent.click(screen.getByRole("button", { name: "Actions" }));
    expect(screen.getByRole("button", { name: "Approve transfer" })).toHaveClass("py-1.5");
    expect(screen.getByRole("button", { name: "Reject transfer" })).toHaveClass("py-1.5");

    expect(mockUseGetNgbTransfersQuery).toHaveBeenLastCalledWith({
      ngb: "USA",
      filter: undefined,
      page: 1,
      pageSize: 25,
    });

    fireEvent.click(screen.getByTitle("2"));

    expect(mockUseGetNgbTransfersQuery).toHaveBeenLastCalledWith({
      ngb: "USA",
      filter: undefined,
      page: 2,
      pageSize: 25,
    });
  });
});
