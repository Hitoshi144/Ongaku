using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ongaku.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderToTracksInPlaylist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "PlaylistTracks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "PlaylistTracks");
        }
    }
}
