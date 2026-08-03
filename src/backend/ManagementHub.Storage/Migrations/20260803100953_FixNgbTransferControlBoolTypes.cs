using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagementHub.Storage.Migrations
{
    /// <inheritdoc />
    public partial class FixNgbTransferControlBoolTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix boolean columns that were incorrectly created as INTEGER on PostgreSQL.
            // This is a no-op on SQLite (which maps boolean to INTEGER natively and doesn't
            // support ALTER COLUMN TYPE), but is required for existing PostgreSQL databases
            // that applied 20260802161817_AddNgbTransferControl before the type was corrected.
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(
                    "ALTER TABLE team_invitations ALTER COLUMN is_internal_transfer TYPE boolean USING (is_internal_transfer::boolean);");
                migrationBuilder.Sql(
                    "ALTER TABLE national_governing_bodies ALTER COLUMN auto_approve_internal_transfers TYPE boolean USING (auto_approve_internal_transfers::boolean);");
                migrationBuilder.Sql(
                    "ALTER TABLE ngb_transfer_approvals ALTER COLUMN is_origin_ngb TYPE boolean USING (is_origin_ngb::boolean);");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(
                    "ALTER TABLE team_invitations ALTER COLUMN is_internal_transfer TYPE integer USING (is_internal_transfer::integer);");
                migrationBuilder.Sql(
                    "ALTER TABLE national_governing_bodies ALTER COLUMN auto_approve_internal_transfers TYPE integer USING (auto_approve_internal_transfers::integer);");
                migrationBuilder.Sql(
                    "ALTER TABLE ngb_transfer_approvals ALTER COLUMN is_origin_ngb TYPE integer USING (is_origin_ngb::integer);");
            }
        }
    }
}
