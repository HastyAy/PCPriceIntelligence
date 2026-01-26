using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Migrations
{
    /// <inheritdoc />
    public partial class ConfigUserToBuilds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "test",
                table: "PCBuilds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_PCBuilds_AspNetUsers_UserId",
                table: "PCBuilds",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PCBuilds_AspNetUsers_UserId",
                table: "PCBuilds");

            migrationBuilder.DropColumn(
                name: "test",
                table: "PCBuilds");
        }
    }
}
