using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feirb.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorAddressStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Addresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: Blocked > Important > Unknown > Known.
            // AddressStatus: Unknown=0, Known=1, Important=2, Blocked=3
            migrationBuilder.Sql(@"
                UPDATE ""Addresses"" SET ""Status"" = CASE
                    WHEN ""IsBlocked"" THEN 3
                    WHEN EXISTS (
                        SELECT 1 FROM ""Contacts"" c
                        WHERE c.""Id"" = ""Addresses"".""ContactId""
                          AND c.""IsImportant"" = TRUE
                    ) THEN 2
                    WHEN ""IsUnknown"" THEN 0
                    ELSE 1
                END;
            ");

            migrationBuilder.DropColumn(
                name: "IsImportant",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "IsUnknown",
                table: "Addresses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsImportant",
                table: "Contacts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "Addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnknown",
                table: "Addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Reverse backfill: derive the three booleans from Status.
            // Important is per-contact, so set IsImportant on the Contact whenever
            // any of its addresses carried Important.
            migrationBuilder.Sql(@"
                UPDATE ""Addresses"" SET ""IsBlocked"" = TRUE WHERE ""Status"" = 3;
                UPDATE ""Addresses"" SET ""IsUnknown"" = TRUE WHERE ""Status"" = 0;
                UPDATE ""Contacts"" SET ""IsImportant"" = TRUE
                    WHERE ""Id"" IN (
                        SELECT DISTINCT ""ContactId"" FROM ""Addresses""
                        WHERE ""Status"" = 2 AND ""ContactId"" IS NOT NULL
                    );
            ");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Addresses");
        }
    }
}
