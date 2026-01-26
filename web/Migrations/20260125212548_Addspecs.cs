using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Migrations
{
    /// <inheritdoc />
    public partial class Addspecs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualPeakWattage",
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
                name: "WeightKg",
                table: "PSUSpecifications");

            migrationBuilder.DropColumn(
                name: "CPUCompatibility",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "DimensionsMM",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "MaxSupportedMemorySpeedMHz",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "MinSupportedMemorySpeedMHz",
                table: "MotherboardSpecs");

            migrationBuilder.DropColumn(
                name: "CoolingType",
                table: "GPUSpecifications");

            migrationBuilder.DropColumn(
                name: "DisplayOutputCount",
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
                name: "Socket",
                table: "CPUSpecifications");

            migrationBuilder.DropColumn(
                name: "AirflowCFM",
                table: "CPUCoolerSpecifications");

            migrationBuilder.DropColumn(
                name: "RadiatorSize",
                table: "CPUCoolerSpecifications");

            migrationBuilder.DropColumn(
                name: "MaxRadiatorHeightMM",
                table: "CaseSpecifications");

            migrationBuilder.RenameColumn(
                name: "VRMPhases",
                table: "MotherboardSpecs",
                newName: "MaxMemorySpeedMHz");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaxMemorySpeedMHz",
                table: "MotherboardSpecs",
                newName: "VRMPhases");

            migrationBuilder.AddColumn<int>(
                name: "ActualPeakWattage",
                table: "PSUSpecifications",
                type: "integer",
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
                name: "CoolingType",
                table: "GPUSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOutputCount",
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

            migrationBuilder.AddColumn<string>(
                name: "Socket",
                table: "CPUSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AirflowCFM",
                table: "CPUCoolerSpecifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RadiatorSize",
                table: "CPUCoolerSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRadiatorHeightMM",
                table: "CaseSpecifications",
                type: "integer",
                nullable: true);
        }
    }
}
