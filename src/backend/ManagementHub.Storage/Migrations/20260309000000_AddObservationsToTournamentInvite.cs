using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagementHub.Storage.Migrations;

/// <inheritdoc />
/// <remarks>
/// This migration adds fantasy tournament registration flags to tournaments and
/// the observations column to tournament_invites.
/// </remarks>
public partial class AddObservationsToTournamentInvite : Migration
{
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<bool>(
			name: "allows_individual_registration",
			table: "tournaments",
			type: "boolean",
			nullable: false,
			defaultValue: false);

		migrationBuilder.AddColumn<bool>(
			name: "allows_team_registration",
			table: "tournaments",
			type: "boolean",
			nullable: false,
			defaultValue: true);

		migrationBuilder.AddColumn<string>(
			name: "observations",
			table: "tournament_invites",
			type: "character varying(1000)",
			maxLength: 1000,
			nullable: true);
	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropColumn(
			name: "observations",
			table: "tournament_invites");

		migrationBuilder.DropColumn(
			name: "allows_individual_registration",
			table: "tournaments");

		migrationBuilder.DropColumn(
			name: "allows_team_registration",
			table: "tournaments");
	}
}
