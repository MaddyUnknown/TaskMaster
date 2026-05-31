using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskMaster.API.Migrations
{
    /// <inheritdoc />
    public partial class Rename_LeaseTimestamp_To_WorkerExpiresAtTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Workers");

            migrationBuilder.RenameColumn(
                name: "LastHeartBeatTimestamp",
                table: "Workers",
                newName: "WorkerExpiresAtTimestamp");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Workers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Workers");

            migrationBuilder.RenameColumn(
                name: "WorkerExpiresAtTimestamp",
                table: "Workers",
                newName: "LastHeartBeatTimestamp");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Workers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
