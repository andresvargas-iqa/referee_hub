using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagementHub.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentTeamRankings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tournament_team_rankings",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    tournament_id = table.Column<long>(type: "INTEGER", nullable: false),
                    team_id = table.Column<long>(type: "INTEGER", nullable: false),
                    ranking_position = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_team_rankings", x => x.id);
                    table.ForeignKey(
                        name: "fk_tournament_team_rankings_team",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tournament_team_rankings_tournament",
                        column: x => x.tournament_id,
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "index_tournament_team_rankings_on_tournament_and_position",
                table: "tournament_team_rankings",
                columns: new[] { "tournament_id", "ranking_position" });

            migrationBuilder.CreateIndex(
                name: "index_tournament_team_rankings_on_tournament_and_team",
                table: "tournament_team_rankings",
                columns: new[] { "tournament_id", "team_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tournament_team_rankings_team_id",
                table: "tournament_team_rankings",
                column: "team_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tournament_team_rankings");
        }
    }
}
