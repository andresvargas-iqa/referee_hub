using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ManagementHub.Storage.Migrations;

/// <inheritdoc />
public partial class AddTeamInvitations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "team_invitations",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                team_id = table.Column<long>(type: "bigint", nullable: false),
                email = table.Column<string>(type: "character varying", nullable: false),
                initiator_user_id = table.Column<long>(type: "bigint", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_team_invitations", x => x.id);
                table.ForeignKey(
                    name: "fk_team_invitations_initiator",
                    column: x => x.initiator_user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_team_invitations_team",
                    column: x => x.team_id,
                    principalTable: "teams",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "index_team_invitations_on_team_id_and_email",
            table: "team_invitations",
            columns: new[] { "team_id", "email" },
            unique: true,
            filter: "revoked_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_team_invitations_initiator_user_id",
            table: "team_invitations",
            column: "initiator_user_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "team_invitations");
    }
}
