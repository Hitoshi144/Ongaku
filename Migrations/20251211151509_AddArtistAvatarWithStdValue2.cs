using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ongaku.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistAvatarWithStdValue2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "Artists",
                type: "text",
                nullable: false,
                defaultValue: "assets/teto_cover.png");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
