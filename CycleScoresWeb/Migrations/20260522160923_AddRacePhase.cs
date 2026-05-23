using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CycleScoresWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddRacePhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phase",
                table: "Race",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Phase",
                table: "Race");
        }
    }
}
