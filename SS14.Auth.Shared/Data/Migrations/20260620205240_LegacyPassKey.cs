using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    public partial class LegacyPassKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedOn",
                table: "DeletedUserIds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 6, 20, 20, 52, 39, 821, DateTimeKind.Utc).AddTicks(9),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2026, 6, 20, 20, 13, 31, 69, DateTimeKind.Utc).AddTicks(9639));

            migrationBuilder.AddColumn<string>(
                name: "LegacyPassKey",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegacyPassKey",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedOn",
                table: "DeletedUserIds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 6, 20, 20, 13, 31, 69, DateTimeKind.Utc).AddTicks(9639),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2026, 6, 20, 20, 52, 39, 821, DateTimeKind.Utc).AddTicks(9));
        }
    }
}
