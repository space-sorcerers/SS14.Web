using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SS14.Auth.Shared.Data.Migrations
{
    public partial class AddBirthdayColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedOn",
                table: "DeletedUserIds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 6, 20, 20, 13, 31, 69, DateTimeKind.Utc).AddTicks(9639),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2026, 6, 16, 9, 14, 39, 255, DateTimeKind.Utc).AddTicks(2141));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedOn",
                table: "DeletedUserIds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 6, 16, 9, 14, 39, 255, DateTimeKind.Utc).AddTicks(2141),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2026, 6, 20, 20, 13, 31, 69, DateTimeKind.Utc).AddTicks(9639));
        }
    }
}
