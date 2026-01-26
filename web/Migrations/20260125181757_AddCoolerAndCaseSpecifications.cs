using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace web.Migrations
{
    /// <inheritdoc />
    public partial class AddCoolerAndCaseSpecifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Components_EAN",
                table: "Components");

            migrationBuilder.DropIndex(
                name: "IX_Components_PartNumber",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "EAN",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "PartNumber",
                table: "Components");

            migrationBuilder.AddColumn<int>(
                name: "ActualPeakWattage",
                table: "PSUSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Aux6PinCount",
                table: "PSUSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Aux8PinCount",
                table: "PSUSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DimensionsMM",
                table: "PSUSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EfficiencyRating",
                table: "PSUSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Has24PinATX",
                table: "PSUSpecifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Has4PinCPU",
                table: "PSUSpecifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Has8PinCPU",
                table: "PSUSpecifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Is80PlusCertified",
                table: "PSUSpecifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RailAmps12V",
                table: "PSUSpecifications",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SATAPowerCount",
                table: "PSUSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "PSUSpecifications",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CPUCompatibility",
                table: "MotherboardSpecs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DimensionsMM",
                table: "MotherboardSpecs",
                type: "text",
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
                name: "M2SlotCount",
                table: "MotherboardSpecs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxMemoryCapacityGB",
                table: "MotherboardSpecs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaxPCIeGeneration",
                table: "MotherboardSpecs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxSupportedMemorySpeedMHz",
                table: "MotherboardSpecs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinSupportedMemorySpeedMHz",
                table: "MotherboardSpecs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PCIeSlots",
                table: "MotherboardSpecs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SATAPortCount",
                table: "MotherboardSpecs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VRMPhases",
                table: "MotherboardSpecs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Aux6PinCount",
                table: "GPUSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Aux8PinCount",
                table: "GPUSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoolingType",
                table: "GPUSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOutputCount",
                table: "GPUSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeightMM",
                table: "GPUSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LengthMM",
                table: "GPUSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PCIeGeneration",
                table: "GPUSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeakWattage",
                table: "GPUSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPCIe16",
                table: "GPUSpecifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WidthMM",
                table: "GPUSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseSpecifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComponentId = table.Column<int>(type: "integer", nullable: false),
                    FormFactor = table.Column<string>(type: "text", nullable: true),
                    MaxGPULengthMM = table.Column<int>(type: "integer", nullable: true),
                    MaxCoolerHeightMM = table.Column<int>(type: "integer", nullable: true),
                    MaxRadiatorHeightMM = table.Column<int>(type: "integer", nullable: true),
                    BayCount35 = table.Column<int>(type: "integer", nullable: true),
                    BayCount25 = table.Column<int>(type: "integer", nullable: true),
                    ExpansionSlots = table.Column<string>(type: "text", nullable: true),
                    HasUSBC = table.Column<bool>(type: "boolean", nullable: false),
                    HasUSB3 = table.Column<bool>(type: "boolean", nullable: false),
                    VolumeLiters = table.Column<decimal>(type: "numeric", nullable: true),
                    DimensionsMM = table.Column<string>(type: "text", nullable: true),
                    HasTemperedGlass = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseSpecifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseSpecifications_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CPUCoolerSpecifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComponentId = table.Column<int>(type: "integer", nullable: false),
                    SocketCompatibility = table.Column<string>(type: "text", nullable: true),
                    MaxTDP = table.Column<int>(type: "integer", nullable: true),
                    HeightMM = table.Column<int>(type: "integer", nullable: true),
                    AirflowCFM = table.Column<int>(type: "integer", nullable: true),
                    NoiseLevelDB = table.Column<int>(type: "integer", nullable: true),
                    IsLiquidCooled = table.Column<bool>(type: "boolean", nullable: false),
                    RadiatorSize = table.Column<string>(type: "text", nullable: true),
                    FanCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CPUCoolerSpecifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CPUCoolerSpecifications_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseSpecifications_ComponentId",
                table: "CaseSpecifications",
                column: "ComponentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CPUCoolerSpecifications_ComponentId",
                table: "CPUCoolerSpecifications",
                column: "ComponentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseSpecifications");

            migrationBuilder.DropTable(
                name: "CPUCoolerSpecifications");

            migrationBuilder.DropColumn(
                name: "ActualPeakWattage",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "Aux6PinCount",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "Aux8PinCount",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "DimensionsMM",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "EfficiencyRating",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "Has24PinATX",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "Has4PinCPU",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "Has8PinCPU",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "Is80PlusCertified",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "RailAmps12V",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "SATAPowerCount",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "CPUCompatibility",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "DimensionsMM",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "HasBluetooth",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "HasWiFi",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "M2SlotCount",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "MaxMemoryCapacityGB",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "MaxPCIeGeneration",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "MaxSupportedMemorySpeedMHz",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "MinSupportedMemorySpeedMHz",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "PCIeSlots",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "SATAPortCount",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "VRMPhases",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "Aux6PinCount",
                table: "GPUSpecifications");

            migrationBuilder.DropColumn(
                name: "Aux8PinCount",
                table: "GPUSpecifications");

            migrationBuilder.DropColumn(
                name: "CoolingType",
                table: "GPUSpecifications");

            migrationBuilder.DropColumn(
                name: "DisplayOutputCount",
                table: "GPUSpecifications");

            migrationBuilder.DropColumn(
                name: "HeightMM",
                table: "GPUSpecifications");

            migrationBuilder.DropColumn(
                name: "LengthMM",
                table: "GPUSpecifications");

            migrationBuilder.DropColumn(
                name: "PCIeGeneration",
                table: "GPUSpecifications");

            migrationBuilder.DropColumn(
                name: "PeakWattage",
                table: "GPUSpecifications");

            migrationBuilder.DropColumn(
                name: "RequiresPCIe16",
                table: "GPUSpecifications");

            migrationBuilder.DropColumn(
                name: "WidthMM",
                table: "GPUSpecifications");

            migrationBuilder.AddColumn<string>(
                name: "EAN",
                table: "Components",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Components",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartNumber",
                table: "Components",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Components_EAN",
                table: "Components",
                column: "EAN");

            migrationBuilder.CreateIndex(
                name: "IX_Components_PartNumber",
                table: "Components",
                column: "PartNumber");
        }
    }
}
