using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vacancy.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hh_search_presets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Query = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DefaultExclusionsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hh_search_presets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vacancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Company = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SalaryFrom = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    SalaryTo = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    SalaryCurrency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RawDataJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vacancies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "parse_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PresetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PresetKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PresetLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SearchQuery = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExclusionsJson = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    QueuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parse_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_parse_jobs_hh_search_presets_PresetId",
                        column: x => x.PresetId,
                        principalTable: "hh_search_presets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_outbox_messages_parse_jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "parse_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "parse_job_vacancies",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    VacancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FoundAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parse_job_vacancies", x => new { x.JobId, x.VacancyId });
                    table.ForeignKey(
                        name: "FK_parse_job_vacancies_parse_jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "parse_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_parse_job_vacancies_vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hh_search_presets_Key",
                table: "hh_search_presets",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_JobId",
                table: "outbox_messages",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt_AvailableAt_CreatedAt",
                table: "outbox_messages",
                columns: new[] { "ProcessedAt", "AvailableAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_parse_job_vacancies_VacancyId",
                table: "parse_job_vacancies",
                column: "VacancyId");

            migrationBuilder.CreateIndex(
                name: "IX_parse_jobs_PresetId",
                table: "parse_jobs",
                column: "PresetId");

            migrationBuilder.CreateIndex(
                name: "IX_parse_jobs_Status_CreatedAt",
                table: "parse_jobs",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_vacancies_Company",
                table: "vacancies",
                column: "Company");

            migrationBuilder.CreateIndex(
                name: "IX_vacancies_Platform_ExternalId",
                table: "vacancies",
                columns: new[] { "Platform", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vacancies_Platform_PublishedAt",
                table: "vacancies",
                columns: new[] { "Platform", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "parse_job_vacancies");

            migrationBuilder.DropTable(
                name: "parse_jobs");

            migrationBuilder.DropTable(
                name: "vacancies");

            migrationBuilder.DropTable(
                name: "hh_search_presets");
        }
    }
}
