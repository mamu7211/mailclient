using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feirb.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMailboxPollIntervalMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PollIntervalMinutes",
                table: "Mailboxes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PollIntervalMinutes",
                table: "Mailboxes",
                type: "integer",
                nullable: false,
                defaultValue: 60);
        }
    }
}
