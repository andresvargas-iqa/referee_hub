using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ManagementHub.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamInviteResponsesAndPlayerActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "index_team_invitations_on_team_id_and_email",
                table: "team_invitations");

            migrationBuilder.AddColumn<DateTime>(
                name: "accepted_at",
                table: "team_invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "declined_at",
                table: "team_invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "responded_by_user_id",
                table: "team_invitations",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "team_player_activities",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    team_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    email = table.Column<string>(type: "character varying", nullable: false),
                    initiator_user_id = table.Column<long>(type: "bigint", nullable: false),
                    activity_type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_player_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_team_player_activities_initiator",
                        column: x => x.initiator_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_team_player_activities_team",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_team_player_activities_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "index_team_invitations_on_team_id_and_email",
                table: "team_invitations",
                columns: new[] { "team_id", "email" },
                unique: true,
                filter: "revoked_at IS NULL AND accepted_at IS NULL AND declined_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_team_invitations_responded_by_user_id",
                table: "team_invitations",
                column: "responded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "index_team_player_activities_on_team_id_and_created_at",
                table: "team_player_activities",
                columns: new[] { "team_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "index_team_player_activities_on_user_id_and_created_at",
                table: "team_player_activities",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_team_player_activities_initiator_user_id",
                table: "team_player_activities",
                column: "initiator_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_team_invitations_responded_by_user",
                table: "team_invitations",
                column: "responded_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_team_invitations_responded_by_user",
                table: "team_invitations");

            migrationBuilder.DropTable(
                name: "team_player_activities");

            migrationBuilder.DropIndex(
                name: "index_team_invitations_on_team_id_and_email",
                table: "team_invitations");

            migrationBuilder.DropIndex(
                name: "IX_team_invitations_responded_by_user_id",
                table: "team_invitations");

            migrationBuilder.DropColumn(
                name: "accepted_at",
                table: "team_invitations");

            migrationBuilder.DropColumn(
                name: "declined_at",
                table: "team_invitations");

            migrationBuilder.DropColumn(
                name: "responded_by_user_id",
                table: "team_invitations");

            migrationBuilder.CreateIndex(
                name: "index_team_invitations_on_team_id_and_email",
                table: "team_invitations",
                columns: new[] { "team_id", "email" },
                unique: true,
                filter: "revoked_at IS NULL");
        }
    }
}
