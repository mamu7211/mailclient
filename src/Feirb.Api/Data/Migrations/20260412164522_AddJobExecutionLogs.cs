using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feirb.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobExecutionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Metadata = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobExecutionLogs_JobExecutions_JobExecutionId",
                        column: x => x.JobExecutionId,
                        principalTable: "JobExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutionLogs_JobExecutionId",
                table: "JobExecutionLogs",
                column: "JobExecutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobExecutionLogs");
        }
    }
}
