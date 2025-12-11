using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ongaku.Migrations
{
    /// <inheritdoc />
    public partial class RemoveArtistAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
               name: "Avatar",
               table: "Artists");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
