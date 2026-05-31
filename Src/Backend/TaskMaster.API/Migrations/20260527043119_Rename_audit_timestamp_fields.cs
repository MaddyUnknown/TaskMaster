using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskMaster.API.Migrations
{
    /// <inheritdoc />
    public partial class Rename_audit_timestamp_fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModifyDate",
                table: "Workers",
                newName: "ModifyDateTime");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Workers",
                newName: "CreatedDateTime");

            migrationBuilder.RenameColumn(
                name: "ModifyDate",
                table: "WorkerCapabalities",
                newName: "ModifyDateTime");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "WorkerCapabalities",
                newName: "CreatedDateTime");

            migrationBuilder.RenameColumn(
                name: "ModifyDate",
                table: "Jobs",
                newName: "ModifyDateTime");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Jobs",
                newName: "CreatedDateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModifyDateTime",
                table: "Workers",
                newName: "ModifyDate");

            migrationBuilder.RenameColumn(
                name: "CreatedDateTime",
                table: "Workers",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "ModifyDateTime",
                table: "WorkerCapabalities",
                newName: "ModifyDate");

            migrationBuilder.RenameColumn(
                name: "CreatedDateTime",
                table: "WorkerCapabalities",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "ModifyDateTime",
                table: "Jobs",
                newName: "ModifyDate");

            migrationBuilder.RenameColumn(
                name: "CreatedDateTime",
                table: "Jobs",
                newName: "CreatedDate");
        }
    }
}
