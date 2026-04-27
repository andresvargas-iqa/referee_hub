import React, { useState, useMemo } from "react";
import { useNavigationParams } from "../../utils/navigationUtils";
import { 
  useCreateTeamInviteMutation,
  useGetTeamManagementQuery,
  useRemovePlayerMutation,
  useRevokeTeamInviteMutation,
} from "../../store/serviceApi";
import { getErrorString } from "../../utils/errorUtils";
import TeamEditModal from "../../components/modals/TeamEditModal/TeamEditModal";
import AddManagerModal from "./AddManagerModal";

const TeamManagement = () => {
  const { teamId } = useNavigationParams<"teamId">();
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isAddManagerModalOpen, setIsAddManagerModalOpen] = useState(false);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteError, setInviteError] = useState<string | null>(null);
  
  const { data: team, error: teamError, isLoading } = useGetTeamManagementQuery(
    { teamId: teamId! },
    { skip: !teamId }
  );

  const [createTeamInvite, { isLoading: isCreatingInvite }] = useCreateTeamInviteMutation();
  const [removePlayer, { isLoading: isRemovingPlayer }] = useRemovePlayerMutation();
  const [revokeTeamInvite, { isLoading: isRevokingInvite }] = useRevokeTeamInviteMutation();

  const handleRemovePlayer = async (playerId: string, playerName: string) => {
    if (!teamId) return;
    
    if (!confirm(`Remove ${playerName} from team?`)) {
      return;
    }

    try {
      await removePlayer({ teamId, playerId }).unwrap();
    } catch (error: any) {
      alert(error?.data || "Failed to remove player. Please try again.");
    }
  };

  const handleCreateInvite = async () => {
    if (!teamId || !inviteEmail.trim()) {
      return;
    }

    setInviteError(null);

    try {
      await createTeamInvite({
        teamId,
        invitePlayerRequest: { email: inviteEmail.trim() },
      }).unwrap();
      setInviteEmail("");
    } catch (error: any) {
      setInviteError(error?.data || "Failed to create invite. Please try again.");
    }
  };

  const handleRevokeInvite = async (invitationId: string, email: string) => {
    if (!teamId) return;

    if (!confirm(`Revoke invitation for ${email}?`)) {
      return;
    }

    try {
      await revokeTeamInvite({ teamId, invitationId }).unwrap();
    } catch (error: any) {
      alert(error?.data || "Failed to revoke invite. Please try again.");
    }
  };

  // Memoize the team object to prevent unnecessary re-renders and form resets
  const teamForModal = useMemo(() => {
    if (!team) return undefined;
    return {
      teamId: team.teamId,
      name: team.name,
      city: team.city,
      state: team.state,
      country: team.country,
      status: team.status,
      groupAffiliation: team.groupAffiliation,
      joinedAt: team.joinedAt,
      socialAccounts: team.socialAccounts,
      description: team.description,
      contactEmail: team.contactEmail,
      logoUri: team.logoUri,
    };
  }, [team]);

  if (isLoading) {
    return (
      <div className="m-auto w-full my-10 px-4 xl:w-3/4 xl:px-0">
        <p>Loading team management...</p>
      </div>
    );
  }

  if (teamError) {
    return (
      <div className="m-auto w-full my-10 px-4 xl:w-3/4 xl:px-0">
        <p className="text-red-500">Error: {getErrorString(teamError)}</p>
      </div>
    );
  }

  if (!team) {
    return (
      <div className="m-auto w-full my-10 px-4 xl:w-3/4 xl:px-0">
        <p>Team not found</p>
      </div>
    );
  }

  return (
    <div className="m-auto w-full my-10 px-4 xl:w-3/4 xl:px-0">
      {/* Team Header */}
      <div className="flex items-center justify-between mb-8">
        <div className="flex items-center">
          {team.logoUri && (
            <img
              src={team.logoUri}
              alt={`${team.name} logo`}
              className="w-20 h-20 object-cover rounded mr-4"
            />
          )}
          <div>
            <h1 className="text-3xl font-bold">{team.name}</h1>
            <p className="text-gray-600">
              {team.city}
              {team.state && `, ${team.state}`}, {team.country}
            </p>
          </div>
        </div>
        
        {/* Actions Button */}
        <div className="relative">
          <button
            className="bg-green text-white px-6 py-3 rounded-lg font-semibold hover:bg-green-700 transition"
            onClick={() => setIsEditModalOpen(true)}
          >
            Edit Team
          </button>
        </div>
      </div>

      {/* Edit Team Modal */}
      {isEditModalOpen && team && (
        <TeamEditModal
          open={isEditModalOpen}
          showClose={true}
          teamId={team.teamId}
          team={teamForModal}
          onClose={() => setIsEditModalOpen(false)}
        />
      )}

      {/* Team Managers Section */}
      <div className="bg-gray-100 rounded-lg p-6 mb-8">
        <h2 className="text-2xl font-semibold mb-4 border-b-2 border-green pb-2">
          Team Managers
        </h2>
        {team.managers && team.managers.length > 0 ? (
          <div className="space-y-2">
            {team.managers.map((manager) => (
              <div key={manager.id} className="flex items-center justify-between bg-white p-3 rounded">
                <div>
                  <span className="font-medium">{manager.name}</span>
                  {manager.email && (
                    <span className="ml-2 text-gray-600 text-sm">({manager.email})</span>
                  )}
                </div>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-gray-500">No managers assigned</p>
        )}
        <button
          className="mt-4 text-green font-semibold hover:underline"
          onClick={() => setIsAddManagerModalOpen(true)}
        >
          + Add Manager
        </button>
      </div>

      {/* Add Manager Modal */}
      {isAddManagerModalOpen && teamId && (
        <AddManagerModal
          teamId={teamId}
          onClose={() => setIsAddManagerModalOpen(false)}
        />
      )}

      {/* Team Players/Members Section */}
      <div className="bg-gray-100 rounded-lg p-6 mb-8">
        <h2 className="text-2xl font-semibold mb-4 border-b-2 border-green pb-2">
          Team Members
        </h2>
        {team.members && team.members.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="min-w-full bg-white rounded">
              <thead className="bg-gray-200">
                <tr>
                  <th className="px-4 py-2 text-left">Name</th>
                  {team.groupAffiliation === "national" && (
                    <th className="px-4 py-2 text-left">Primary Team</th>
                  )}
                  <th className="px-4 py-2 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {team.members.map((member) => (
                  <tr key={member.userId} className="border-t">
                    <td className="px-4 py-3 font-medium">{member.name}</td>
                    {team.groupAffiliation === "national" && (
                      <td className="px-4 py-3 text-gray-600">
                        {member.primaryTeamName || "—"}
                      </td>
                    )}
                    <td className="px-4 py-3 text-right">
                      <button
                        className="text-red-600 hover:underline disabled:opacity-50"
                        onClick={() => handleRemovePlayer(member.userId, member.name)}
                        disabled={isRemovingPlayer}
                      >
                        Remove
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="text-gray-500">No members</p>
        )}
      </div>

      {/* Pending Invites Section */}
      <div className="bg-gray-100 rounded-lg p-6">
        <div className="flex items-center justify-between mb-4 border-b-2 border-green pb-2 gap-4">
          <h2 className="text-2xl font-semibold">Pending Invitations</h2>
          <div className="flex gap-2 w-full max-w-lg">
            <input
              type="email"
              className="flex-1 px-3 py-2 border border-gray-300 rounded"
              placeholder="player@example.com"
              value={inviteEmail}
              onChange={(event) => setInviteEmail(event.target.value)}
            />
            <button
              className="bg-green text-white px-4 py-2 rounded font-semibold hover:bg-green-700 disabled:opacity-50"
              onClick={handleCreateInvite}
              disabled={isCreatingInvite || !inviteEmail.trim()}
            >
              {isCreatingInvite ? "Inviting..." : "Invite Player"}
            </button>
          </div>
        </div>
        {inviteError && <p className="mb-4 text-sm text-red-600">{inviteError}</p>}
        {team.pendingInvites && team.pendingInvites.length > 0 ? (
          <div className="space-y-2">
            {team.pendingInvites.map((invite) => (
              <div key={invite.invitationId} className="bg-white p-3 rounded flex items-center justify-between gap-4">
                <div>
                  <p className="font-medium">{invite.email}</p>
                  <p className="text-sm text-gray-600">
                    Invited {invite.createdAt ? new Date(invite.createdAt).toLocaleDateString() : "recently"}
                    {invite.invitedByName ? ` by ${invite.invitedByName}` : ""}
                  </p>
                </div>
                <button
                  className="text-red-600 hover:underline disabled:opacity-50"
                  onClick={() => handleRevokeInvite(invite.invitationId, invite.email)}
                  disabled={isRevokingInvite}
                >
                  Revoke
                </button>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-gray-500">No pending invitations</p>
        )}
      </div>
    </div>
  );
};

export default TeamManagement;
