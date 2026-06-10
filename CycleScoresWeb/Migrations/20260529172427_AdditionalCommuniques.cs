using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CycleScoresWeb.Migrations
{
    /// <inheritdoc />
    public partial class AdditionalCommuniques : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RaceId",
                table: "CommuniqueSet",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommuniqueSet_RaceId",
                table: "CommuniqueSet",
                column: "RaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommuniqueSet_Race_RaceId",
                table: "CommuniqueSet",
                column: "RaceId",
                principalTable: "Race",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommuniqueSet_Race_RaceId",
                table: "CommuniqueSet");

            migrationBuilder.DropIndex(
                name: "IX_CommuniqueSet_RaceId",
                table: "CommuniqueSet");

            migrationBuilder.DropColumn(
                name: "RaceId",
                table: "CommuniqueSet");
        }
    }
}
