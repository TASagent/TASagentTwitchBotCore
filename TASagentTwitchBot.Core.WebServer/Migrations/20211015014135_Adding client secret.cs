using Microsoft.EntityFrameworkCore.Migrations;

namespace TASagentTwitchBot.Core.WebServer.Migrations
{
    public partial class Addingclientsecret : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubscriptionSecret",
                schema: "Identity",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionSecret",
                schema: "Identity",
                table: "User");
        }
    }
}
