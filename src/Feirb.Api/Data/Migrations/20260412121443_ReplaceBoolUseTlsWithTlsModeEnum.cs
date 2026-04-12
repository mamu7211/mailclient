using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feirb.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceBoolUseTlsWithTlsModeEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new string columns with defaults
            migrationBuilder.AddColumn<string>(
                name: "TlsMode",
                table: "SmtpSettings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "ImapTlsMode",
                table: "Mailboxes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "SmtpTlsMode",
                table: "Mailboxes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "None");

            // Migrate existing data: true -> "Auto", false -> "None"
            migrationBuilder.Sql("""
                UPDATE "SmtpSettings" SET "TlsMode" = CASE WHEN "UseTls" THEN 'Auto' ELSE 'None' END;
                UPDATE "Mailboxes" SET "ImapTlsMode" = CASE WHEN "ImapUseTls" THEN 'Auto' ELSE 'None' END;
                UPDATE "Mailboxes" SET "SmtpTlsMode" = CASE WHEN "SmtpUseTls" THEN 'Auto' ELSE 'None' END;
                """);

            // Drop old bool columns
            migrationBuilder.DropColumn(
                name: "UseTls",
                table: "SmtpSettings");

            migrationBuilder.DropColumn(
                name: "ImapUseTls",
                table: "Mailboxes");

            migrationBuilder.DropColumn(
                name: "SmtpUseTls",
                table: "Mailboxes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore bool columns
            migrationBuilder.AddColumn<bool>(
                name: "UseTls",
                table: "SmtpSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ImapUseTls",
                table: "Mailboxes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmtpUseTls",
                table: "Mailboxes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Migrate data back: anything not "None" -> true
            migrationBuilder.Sql("""
                UPDATE "SmtpSettings" SET "UseTls" = "TlsMode" <> 'None';
                UPDATE "Mailboxes" SET "ImapUseTls" = "ImapTlsMode" <> 'None';
                UPDATE "Mailboxes" SET "SmtpUseTls" = "SmtpTlsMode" <> 'None';
                """);

            // Drop new string columns
            migrationBuilder.DropColumn(
                name: "TlsMode",
                table: "SmtpSettings");

            migrationBuilder.DropColumn(
                name: "ImapTlsMode",
                table: "Mailboxes");

            migrationBuilder.DropColumn(
                name: "SmtpTlsMode",
                table: "Mailboxes");
        }
    }
}
