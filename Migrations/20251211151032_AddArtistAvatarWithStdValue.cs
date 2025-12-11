using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ongaku.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistAvatarWithStdValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
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
