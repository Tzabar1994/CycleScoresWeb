using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CycleScoresWeb.Migrations
{
    /// <inheritdoc />
    public partial class CommuniqueReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ResultCommuniqueID",
                table: "Race",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StartCommuniqueID",
                table: "Race",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommuniqueSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommuniqueTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommuniqueID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleEventId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommuniqueSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommuniqueSet_Events_CycleEventId",
                        column: x => x.CycleEventId,
                        principalTable: "Events",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommuniqueSet_CycleEventId",
                table: "CommuniqueSet",
                column: "CycleEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommuniqueSet");

            migrationBuilder.DropColumn(
                name: "ResultCommuniqueID",
                table: "Race");

            migrationBuilder.DropColumn(
                name: "StartCommuniqueID",
                table: "Race");
        }
    }
}
