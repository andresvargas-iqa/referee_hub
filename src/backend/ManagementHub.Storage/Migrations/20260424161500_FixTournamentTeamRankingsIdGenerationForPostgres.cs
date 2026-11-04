using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace ManagementHub.Storage.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ManagementHubDbContext))]
    [Migration("20260424161500_FixTournamentTeamRankingsIdGenerationForPostgres")]
    public partial class FixTournamentTeamRankingsIdGenerationForPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!this.ActiveProvider.Contains("Npgsql"))
            {
                return;
            }

            // Ensure PostgreSQL has a sequence-backed default for the PK column.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_class
        WHERE relname = 'tournament_team_rankings_id_seq'
    ) THEN
        CREATE SEQUENCE tournament_team_rankings_id_seq;
    END IF;
END
$$;");

            migrationBuilder.Sql(@"
ALTER SEQUENCE tournament_team_rankings_id_seq
OWNED BY tournament_team_rankings.id;");

            migrationBuilder.Sql(@"
ALTER TABLE tournament_team_rankings
ALTER COLUMN id SET DEFAULT nextval('tournament_team_rankings_id_seq');");

            migrationBuilder.Sql(@"
SELECT setval(
    'tournament_team_rankings_id_seq',
    COALESCE((SELECT MAX(id) FROM tournament_team_rankings), 1),
    (SELECT COUNT(*) > 0 FROM tournament_team_rankings)
);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (!this.ActiveProvider.Contains("Npgsql"))
            {
                return;
            }

            migrationBuilder.Sql(@"
ALTER TABLE tournament_team_rankings
ALTER COLUMN id DROP DEFAULT;");
        }
    }
}
