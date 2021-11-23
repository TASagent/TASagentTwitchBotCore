using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TASagentTwitchBot.Core.WebServer.Migrations;

public partial class RemovingUnusedSubscriptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Subscriptions",
            schema: "Identity");

        migrationBuilder.Sql(@"UPDATE [Identity].[User] SET SubscriptionSecret='' WHERE SubscriptionSecret IS NULL");

        migrationBuilder.AlterColumn<string>(
            name: "SubscriptionSecret",
            schema: "Identity",
            table: "User",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "SubscriptionSecret",
            schema: "Identity",
            table: "User",
            type: "nvarchar(max)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.CreateTable(
            name: "Subscriptions",
            schema: "Identity",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SubscriberId1 = table.Column<string>(type: "nvarchar(450)", nullable: true),
                SubscriberId = table.Column<int>(type: "int", nullable: false),
                SubscriptionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                SubscriptionType = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Subscriptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Subscriptions_User_SubscriberId1",
                    column: x => x.SubscriberId1,
                    principalSchema: "Identity",
                    principalTable: "User",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_Subscriptions_SubscriberId1",
            schema: "Identity",
            table: "Subscriptions",
            column: "SubscriberId1");
    }
}
