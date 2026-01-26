using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace web.Migrations
{
    /// <inheritdoc />
    public partial class FinalSpecs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompatibilityRules");

            migrationBuilder.DropTable(
                name: "ScrapingJobs");

            migrationBuilder.DropTable(
                name: "SearchQueries");

            migrationBuilder.DropColumn(
                name: "Aux8PinCount",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "HasBluetooth",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "HasWiFi",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "SATAPortCount",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "NoiseLevelDB",
                table: "CPUCoolerSpecifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Aux8PinCount",
                table: "PSUSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBluetooth",
                table: "MotherboardSpecs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasWiFi",
                table: "MotherboardSpecs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SATAPortCount",
                table: "MotherboardSpecs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NoiseLevelDB",
                table: "CPUCoolerSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompatibilityRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConditionJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompatibilityRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScrapingJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ComponentsScraped = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Errors = table.Column<int>(type: "integer", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    PricesUpdated = table.Column<int>(type: "integer", nullable: false),
                    Retailer = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapingJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ParsedIntentJson = table.Column<string>(type: "text", nullable: true),
                    Query = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResultCount = table.Column<int>(type: "integer", nullable: false),
                    SearchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchQueries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompatibilityRules_SourceType_TargetType",
                table: "CompatibilityRules",
                columns: new[] { "SourceType", "TargetType" });

            migrationBuilder.CreateIndex(
                name: "IX_ScrapingJobs_Retailer",
                table: "ScrapingJobs",
                column: "Retailer");

            migrationBuilder.CreateIndex(
                name: "IX_ScrapingJobs_StartedAt",
                table: "ScrapingJobs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueries_SearchedAt",
                table: "SearchQueries",
                column: "SearchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueries_UserId",
                table: "SearchQueries",
                column: "UserId");
        }
    }
}
