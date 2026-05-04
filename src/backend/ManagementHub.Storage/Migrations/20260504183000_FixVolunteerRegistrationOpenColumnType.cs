using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace ManagementHub.Storage.Migrations
{
	[DbContext(typeof(ManagementHubDbContext))]
	[Migration("20260504183000_FixVolunteerRegistrationOpenColumnType")]
	/// <inheritdoc />
	public partial class FixVolunteerRegistrationOpenColumnType : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql(@"
				ALTER TABLE tournaments
				ALTER COLUMN is_volunteer_registration_open DROP DEFAULT;");

			migrationBuilder.Sql(@"
				ALTER TABLE tournaments
				ALTER COLUMN is_volunteer_registration_open TYPE boolean
				USING (is_volunteer_registration_open <> 0);");

			migrationBuilder.Sql(@"
				ALTER TABLE tournaments
				ALTER COLUMN is_volunteer_registration_open SET DEFAULT FALSE;");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql(@"
				ALTER TABLE tournaments
				ALTER COLUMN is_volunteer_registration_open DROP DEFAULT;");

			migrationBuilder.Sql(@"
				ALTER TABLE tournaments
				ALTER COLUMN is_volunteer_registration_open TYPE INTEGER
				USING (CASE WHEN is_volunteer_registration_open THEN 1 ELSE 0 END);");

			migrationBuilder.Sql(@"
				ALTER TABLE tournaments
				ALTER COLUMN is_volunteer_registration_open SET DEFAULT 0;");
		}
	}
}