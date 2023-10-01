using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DeleteEventsColumnFromEventVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_EventVideos_EventVideoId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_EventVideoId",
                table: "Events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Events_EventVideoId",
                table: "Events",
                column: "EventVideoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_EventVideos_EventVideoId",
                table: "Events",
                column: "EventVideoId",
                principalTable: "EventVideos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
