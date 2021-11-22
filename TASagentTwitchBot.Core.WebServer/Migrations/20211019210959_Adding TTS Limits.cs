using Microsoft.EntityFrameworkCore.Migrations;

namespace TASagentTwitchBot.Core.WebServer.Migrations;

public partial class AddingTTSLimits : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "MonthlyTTSLimit",
            schema: "Identity",
            table: "User",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "MonthlyTTSUsage",
            schema: "Identity",
            table: "User",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MonthlyTTSLimit",
            schema: "Identity",
            table: "User");

        migrationBuilder.DropColumn(
            name: "MonthlyTTSUsage",
            schema: "Identity",
            table: "User");
    }
}
