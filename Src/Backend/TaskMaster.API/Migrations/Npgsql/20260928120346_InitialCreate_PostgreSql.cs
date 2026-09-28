using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TaskMaster.API.Migrations.Npgsql
{
    /// <inheritdoc />
    public partial class InitialCreate_PostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Schema = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifyDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemActivities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityType = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifyDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkerPublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastHeartBeatTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WorkerExpiresAtTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifyDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobPublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedWorkerId = table.Column<long>(type: "bigint", nullable: true),
                    JobTypeId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifyDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jobs_Workers_AssignedWorkerId",
                        column: x => x.AssignedWorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkerCapabilities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkerId = table.Column<long>(type: "bigint", nullable: false),
                    JobTypeId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifyDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerCapabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerCapabilities_JobTypes_JobTypeId",
                        column: x => x.JobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerCapabilities_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_AssignedWorkerId",
                table: "Jobs",
                column: "AssignedWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobPublicId",
                table: "Jobs",
                column: "JobPublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobTypeId_Status",
                table: "Jobs",
                columns: new[] { "JobTypeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JobTypes_Name_Version",
                table: "JobTypes",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemActivities_CreatedDateTime",
                table: "SystemActivities",
                column: "CreatedDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerCapabilities_JobTypeId",
                table: "WorkerCapabilities",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerCapabilities_WorkerId_JobTypeId",
                table: "WorkerCapabilities",
                columns: new[] { "WorkerId", "JobTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workers_Status_WorkerExpiresAtTimestamp",
                table: "Workers",
                columns: new[] { "Status", "WorkerExpiresAtTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Workers_WorkerName",
                table: "Workers",
                column: "WorkerName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workers_WorkerPublicId",
                table: "Workers",
                column: "WorkerPublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "SystemActivities");

            migrationBuilder.DropTable(
                name: "WorkerCapabilities");

            migrationBuilder.DropTable(
                name: "JobTypes");

            migrationBuilder.DropTable(
                name: "Workers");
        }
    }
}
