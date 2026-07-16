using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskMaster.API.Migrations
{
    /// <inheritdoc />
    public partial class Add_JobAndWorkerPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_JobTypeId",
                table: "Jobs");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_Status_WorkerExpiresAtTimestamp",
                table: "Workers",
                columns: new[] { "Status", "WorkerExpiresAtTimestamp" })
                .Annotation("SqlServer:Include", new[] { "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobTypeId_Status",
                table: "Jobs",
                columns: new[] { "JobTypeId", "Status" })
                .Annotation("SqlServer:Include", new[] { "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workers_Status_WorkerExpiresAtTimestamp",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_JobTypeId_Status",
                table: "Jobs");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobTypeId",
                table: "Jobs",
                column: "JobTypeId");
        }
    }
}
