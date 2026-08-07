using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SsoAuthenticationServer.Migrations
{
    /// <inheritdoc />
    public partial class m2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExpired",
                table: "SSOTokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExpired",
                table: "SSOTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
