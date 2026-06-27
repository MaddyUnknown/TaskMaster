using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskMaster.API.Migrations
{
    /// <inheritdoc />
    public partial class Add_JobType_Description_Worker_DisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkerName",
                table: "Workers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastHeartBeatTimestamp",
                table: "Workers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "WorkerDisplayName",
                table: "Workers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "JobTypes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_WorkerName",
                table: "Workers",
                column: "WorkerName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workers_WorkerName",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "LastHeartBeatTimestamp",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "WorkerDisplayName",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "JobTypes");

            migrationBuilder.AlterColumn<string>(
                name: "WorkerName",
                table: "Workers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
