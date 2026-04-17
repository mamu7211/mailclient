using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feirb.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLogRetentionCleanupJobSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "JobSettings",
                columns: new[] { "Id", "Configuration", "Cron", "Description", "Enabled", "JobName", "JobType", "LastRunAt", "LastStatus", "ResourceId", "ResourceType", "RowVersion", "UserId" },
                values: new object[] { new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"), "{\"retentionDays\":30}", "0 0 3 * * ?", "Deletes old job execution logs based on configurable retention threshold.", true, "Log Retention Cleanup", "log-retention-cleanup", null, null, null, null, new Guid("00000000-0000-0000-0000-000000000002"), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "JobSettings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"));
        }
    }
}
