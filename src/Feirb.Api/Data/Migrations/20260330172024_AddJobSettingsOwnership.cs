using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feirb.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSettingsOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobType",
                table: "JobSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResourceId",
                table: "JobSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceType",
                table: "JobSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "JobSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "JobSettings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                columns: new[] { "JobType", "ResourceId", "ResourceType", "UserId" },
                values: new object[] { "classification", null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_JobSettings_JobType_ResourceId",
                table: "JobSettings",
                columns: new[] { "JobType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_JobSettings_UserId",
                table: "JobSettings",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobSettings_Users_UserId",
                table: "JobSettings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobSettings_Users_UserId",
                table: "JobSettings");

            migrationBuilder.DropIndex(
                name: "IX_JobSettings_JobType_ResourceId",
                table: "JobSettings");

            migrationBuilder.DropIndex(
                name: "IX_JobSettings_UserId",
                table: "JobSettings");

            migrationBuilder.DropColumn(
                name: "JobType",
                table: "JobSettings");

            migrationBuilder.DropColumn(
                name: "ResourceId",
                table: "JobSettings");

            migrationBuilder.DropColumn(
                name: "ResourceType",
                table: "JobSettings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "JobSettings");
        }
    }
}
