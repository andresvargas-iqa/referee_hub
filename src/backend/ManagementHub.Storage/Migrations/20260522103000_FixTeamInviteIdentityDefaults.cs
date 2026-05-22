using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagementHub.Storage.Migrations
{
    /// <inheritdoc />
    public partial class FixTeamInviteIdentityDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'team_invitations'
          AND column_name = 'id'
          AND column_default IS NULL
    ) THEN
        CREATE SEQUENCE IF NOT EXISTS team_invitations_id_seq;
        ALTER TABLE team_invitations ALTER COLUMN id SET DEFAULT nextval('team_invitations_id_seq');
        PERFORM setval('team_invitations_id_seq', COALESCE((SELECT MAX(id) FROM team_invitations), 0) + 1, false);
        ALTER SEQUENCE team_invitations_id_seq OWNED BY team_invitations.id;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'team_player_activities'
          AND column_name = 'id'
          AND column_default IS NULL
    ) THEN
        CREATE SEQUENCE IF NOT EXISTS team_player_activities_id_seq;
        ALTER TABLE team_player_activities ALTER COLUMN id SET DEFAULT nextval('team_player_activities_id_seq');
        PERFORM setval('team_player_activities_id_seq', COALESCE((SELECT MAX(id) FROM team_player_activities), 0) + 1, false);
        ALTER SEQUENCE team_player_activities_id_seq OWNED BY team_player_activities.id;
    END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'team_invitations' AND column_name = 'id'
    ) THEN
        ALTER TABLE team_invitations ALTER COLUMN id DROP DEFAULT;
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'team_player_activities' AND column_name = 'id'
    ) THEN
        ALTER TABLE team_player_activities ALTER COLUMN id DROP DEFAULT;
    END IF;
END $$;
");
        }
    }
}
