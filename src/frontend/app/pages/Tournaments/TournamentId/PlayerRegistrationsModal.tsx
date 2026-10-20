import React, { forwardRef, useImperativeHandle, useState } from "react";
import { Dialog, DialogPanel, DialogTitle } from "@headlessui/react";
import { faXmark, faTrash } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import ActionButtonPair from "../../../components/ActionButtonPair";
import CustomAlert from "../../../components/CustomAlert";
import StatusBadge from "../../../components/StatusBadge";
import { useAlert } from "../../../hooks/useAlert";
import {
  TournamentInviteViewModel,
  useDeleteInviteMutation,
  useGetTournamentInvitesQuery,
  useRespondToInviteMutation,
} from "../../../store/serviceApi";

export interface PlayerRegistrationsModalRef {
  open: (tournamentId: string, tournamentName: string) => void;
}

const PlayerRegistrationsModal = forwardRef<PlayerRegistrationsModalRef>((_props, ref) => {
  const [isOpen, setIsOpen] = useState(false);
  const [tournamentId, setTournamentId] = useState("");
  const [tournamentName, setTournamentName] = useState("");
  const { alertState, showAlert, hideAlert } = useAlert();
  const { data: invites, isFetching, isError } = useGetTournamentInvitesQuery(
    { tournamentId },
    { skip: !isOpen || !tournamentId },
  );
  const [respondToInvite, { isLoading: isResponding }] = useRespondToInviteMutation();
  const [deleteInvite, { isLoading: isDeleting }] = useDeleteInviteMutation();
  const isSubmitting = isResponding || isDeleting;
  const playerInvites = invites?.filter((invite) => invite.participantType === "player") ?? [];

  useImperativeHandle(ref, () => ({
    open: (nextTournamentId, nextTournamentName) => {
      setTournamentId(nextTournamentId);
      setTournamentName(nextTournamentName);
      hideAlert();
      setIsOpen(true);
    },
  }));

  async function respond(invite: TournamentInviteViewModel, approved: boolean) {
    if (!invite.participantId) return;
    try {
      await respondToInvite({
        tournamentId,
        participantId: invite.participantId,
        participantType: "player",
        inviteResponseModel: { approved },
      }).unwrap();
      showAlert(approved ? "Individual registration approved." : "Individual registration declined.", "success");
    } catch {
      showAlert("Failed to review registration. Please try again.", "error");
    }
  }

  async function remove(invite: TournamentInviteViewModel) {
    if (!invite.participantId || !confirm(`Remove ${invite.participantName}'s registration?`)) return;
    try {
      await deleteInvite({ tournamentId, participantId: invite.participantId }).unwrap();
      showAlert("Individual registration removed.", "success");
    } catch {
      showAlert("Failed to remove registration. Please try again.", "error");
    }
  }

  return (
    <Dialog open={isOpen} onClose={() => setIsOpen(false)} className="relative z-50">
      <div className="fixed inset-0 bg-black bg-opacity-30" aria-hidden="true" />
      <div className="fixed inset-0 flex items-center justify-center p-4 overflow-y-auto">
        <DialogPanel className="relative w-full max-w-2xl rounded-lg bg-white shadow-xl my-8 overflow-y-auto" style={{ maxHeight: "90vh" }}>
          <div className="p-6 border-b border-gray-200 flex items-start justify-between gap-4">
            <div className="min-w-0">
              <DialogTitle className="text-xl font-semibold text-gray-900">Individual Registrations</DialogTitle>
              <p className="text-sm text-gray-600 break-words">{tournamentName}</p>
            </div>
            <button type="button" aria-label="Close individual registrations" title="Close" onClick={() => setIsOpen(false)} className="text-gray-600 p-2">
              <FontAwesomeIcon icon={faXmark} />
            </button>
          </div>
          <div className="p-6">
            {alertState.isVisible && <CustomAlert message={alertState.message} type={alertState.type} onClose={hideAlert} />}
            {isError ? <p role="alert">Unable to load individual registrations.</p>
              : isFetching && !invites ? <p role="status">Loading registrations...</p>
                : playerInvites.length === 0 ? <p>No individual registrations.</p>
                  : <ul className="divide-y divide-gray-200">
                    {playerInvites.map((invite) => (
                      <li key={invite.participantId} className="py-4 flex flex-wrap items-center justify-between gap-4">
                        <div className="min-w-0">
                          <p className="font-semibold text-gray-900 break-words">{invite.participantName}</p>
                          <StatusBadge status={invite.status} />
                        </div>
                        <div className="flex items-center gap-3">
                          {invite.status === "pending" && invite.tournamentManagerApproval?.status === "pending" && (
                            <ActionButtonPair onAccept={() => respond(invite, true)} onDecline={() => respond(invite, false)} isLoading={isSubmitting} size="sm" />
                          )}
                          <button type="button" aria-label={`Remove ${invite.participantName}'s registration`} title="Remove registration" onClick={() => remove(invite)} disabled={isSubmitting} className="text-red-600 p-2 disabled:opacity-50">
                            <FontAwesomeIcon icon={faTrash} />
                          </button>
                        </div>
                      </li>
                    ))}
                  </ul>}
          </div>
        </DialogPanel>
      </div>
    </Dialog>
  );
});

export default PlayerRegistrationsModal;